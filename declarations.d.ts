// =============================================================================
// MumbleManager
// Author:  Gerald Hull, W1VE
// Date:    April 14, 2026
// License: MIT License
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.
// =============================================================================

import { useEffect, useRef } from 'react'
import * as signalR from '@microsoft/signalr'
import { getToken } from '../store/auth'

export type StatusHandler = (msg: string) => void

// Derive a stable per-browser client key from the JWT token
// (same logic as the backend SessionRegistry.ClientKey)
function getClientKey(): string {
  const token = getToken() ?? ''
  return token.length > 16 ? token.slice(-16) : token || 'default'
}

// Do NOT create connection at module load — only when explicitly needed
let _connection: signalR.HubConnection | null = null
let _started = false

export function resetConnection() {
  _started = false
  if (_connection) {
    _connection.stop().catch(() => {})
    _connection = null
  }
}

export function useSignalR(
  hostId: string | null,
  onStatus:        (msg: string) => void,
  onConnected?:    (data: { hostId: string; serverCount: number }) => void,
  onDisconnected?: (hostId: string) => void,
  onError?:        (msg: string) => void,
) {
  const cbRef = useRef({ onStatus, onConnected, onDisconnected, onError })
  cbRef.current = { onStatus, onConnected, onDisconnected, onError }

  useEffect(() => {
    const token = getToken()
    if (!token) return   // hard stop — no token, no connection

    // Create connection fresh each time we have a token
    if (!_connection) {
      _connection = new signalR.HubConnectionBuilder()
        .withUrl('/hubs/status', {
          accessTokenFactory: () => getToken() ?? '',
        })
        .configureLogging(signalR.LogLevel.Warning)
        .build()
      _started = false
    }

    const conn = _connection
    const clientKey = getClientKey()

    const onStatusMsg = (msg: string)   => cbRef.current.onStatus(msg)
    const onConn      = (data: unknown) => cbRef.current.onConnected?.(data as any)
    const onDisconn   = (id: string)    => cbRef.current.onDisconnected?.(id)
    const onErr       = (msg: string)   => cbRef.current.onError?.(msg)

    conn.on('status',       onStatusMsg)
    conn.on('connected',    onConn)
    conn.on('disconnected', onDisconn)
    conn.on('error',        onErr)

    const start = async () => {
      if (!getToken()) return
      if (_started) {
        if (hostId) conn.invoke('JoinHostGroup', hostId, clientKey).catch(() => {})
        return
      }
      try {
        await conn.start()
        _started = true
        if (hostId) await conn.invoke('JoinHostGroup', hostId, clientKey).catch(() => {})
      } catch (e) {
        console.warn('SignalR start failed:', e)
      }
    }

    start()

    return () => {
      conn.off('status',       onStatusMsg)
      conn.off('connected',    onConn)
      conn.off('disconnected', onDisconn)
      conn.off('error',        onErr)
      if (hostId && _started) {
        conn.invoke('LeaveHostGroup', hostId, clientKey).catch(() => {})
      }
    }
  }, [hostId])
}

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

import { useState, useEffect } from 'react'
import { hostsApi, connectionApi } from '../api'
import { useAuthStore, getToken } from '../store/auth'
import { useAppStore } from '../store'
import { useSignalR } from '../hooks/useSignalR'
import HostDialog from './HostDialog'
import type { SshHostEntry } from '../types'
import styles from './HostPanel.module.css'

export default function HostPanel() {
  const {
    hosts, setHosts, selectHost, selectServer, selectedHostId,
    appendStatus, setHostConnected,
  } = useAppStore()

  const token = useAuthStore((s) => s.token)
  const [showDialog,    setShowDialog]    = useState(false)
  const [editTarget,    setEditTarget]    = useState<SshHostEntry | null>(null)
  const [connecting,    setConnecting]    = useState<string | null>(null)
  const [restarting,    setRestarting]    = useState(false)

  useEffect(() => {
    if (!token) return
    let cancelled = false
    hostsApi.list().then((fetched) => {
      if (cancelled) return
      setHosts(fetched.map((h) => ({ ...h, isConnected: false, cachedServers: [] })))
      fetched.forEach(async (h) => {
        try {
          const status = await connectionApi.status(h.id)
          if (!cancelled && status.connected) setHostConnected(h.id, true)
        } catch { }
      })
    }).catch((e) => { if (!cancelled) console.error(e) })
    return () => { cancelled = true }
  }, [token])

  useSignalR(
    connecting,
    (msg) => appendStatus(msg),
    ({ serverCount }) => {
      if (!getToken()) return
      hostsApi.list().then((fetched) => {
        setHosts(fetched.map((h) => ({
          ...h,
          isConnected: h.id === connecting ? true : h.isConnected,
          cachedServers: h.id === connecting ? h.cachedServers : [],
        })))
      })
      appendStatus(`✓ Connected (${serverCount} server(s))`)
      setConnecting(null)
    },
    (hostId) => {
      setHostConnected(hostId, false, [])
      selectServer(null)
      setConnecting(null)
      appendStatus('Disconnected.')
    },
    (msg) => {
      appendStatus(`⚠ ${msg}`)
      setConnecting(null)
    },
  )

  async function handleConnect(host: SshHostEntry) {
    if (!getToken()) return
    if (host.isConnected) {
      await connectionApi.disconnect(host.id)
      setHostConnected(host.id, false, [])
      selectServer(null)
      appendStatus(`Disconnected from ${host.displayName}.`)
    } else {
      setConnecting(host.id)
      appendStatus(`Connecting to ${host.displayName}…`)
      connectionApi.connect(host.id).catch((e) => {
        appendStatus(`⚠ ${e.message}`)
        setConnecting(null)
      })
    }
  }

  async function handleRestartMumble(host: SshHostEntry) {
    setRestarting(true)
    try {
      appendStatus(`Restarting mumble-server on ${host.displayName}…`)
      const res = await hostsApi.restartMumble(host.id)
      appendStatus(`✓ Restart complete — ${res.output?.trim() || 'ok'}`)
    } catch (e: any) {
      appendStatus(`⚠ ${e.message}`)
    } finally {
      setRestarting(false)
    }
  }

  async function handleDelete(host: SshHostEntry) {
    if (!confirm(`Remove host "${host.displayName}"?`)) return
    await hostsApi.remove(host.id)
    setHosts(await hostsApi.list())
    if (selectedHostId === host.id) {
      selectHost(null)
      selectServer(null)
    }
  }

  return (
    <div className={styles.panel}>
      <div className="section-header">SSH Hosts</div>
      <ul className={styles.list}>
        {hosts.map((h) => (
          <li
            key={h.id}
            className={`${styles.item} ${selectedHostId === h.id ? styles.selected : ''}`}
            onClick={() => selectHost(h.id)}
          >
            <span className={`dot ${h.isConnected ? 'dot--on' : 'dot--off'}`} />
            <span className={styles.info}>
              <span className={styles.name}>{h.displayName}</span>
              <span className={styles.addr}>{h.host}:{h.sshPort}</span>
            </span>
          </li>
        ))}
        {hosts.length === 0 && (
          <li className={styles.empty}>No hosts configured</li>
        )}
      </ul>
      {selectedHostId && (() => {
        const host = hosts.find((h) => h.id === selectedHostId)!
        if (!host) return null
        const busy = connecting === host.id
        return (
          <div className={styles.actions}>
            <button
              className={host.isConnected ? 'btn-danger' : 'btn-dim'}
              disabled={busy}
              onClick={() => handleConnect(host)}
            >
              {busy ? '…' : host.isConnected ? '✕ Disconnect' : '⇄ Connect'}
            </button>
            <button className="btn-ghost" onClick={() => { setEditTarget(host); setShowDialog(true) }}>
              Edit
            </button>
            <button className="btn-danger" onClick={() => handleDelete(host)}>
              Remove
            </button>
          </div>
        )
      })()}
      <div className={styles.footer}>
        {(() => {
          const host = hosts.find((h) => h.id === selectedHostId)
          return (
            <button
              className="btn-accent"
              disabled={!selectedHostId || !host?.isConnected || restarting}
              onClick={() => host && handleRestartMumble(host)}
            >
              {restarting ? '…' : 'RESTART'}
            </button>
          )
        })()}
        <button className="btn-accent" onClick={() => { setEditTarget(null); setShowDialog(true) }}>
          + Add Host
        </button>
      </div>
      {showDialog && (
        <HostDialog
          existing={editTarget}
          onClose={() => setShowDialog(false)}
          onSaved={async () => { setShowDialog(false); setHosts(await hostsApi.list()) }}
        />
      )}
    </div>
  )
}

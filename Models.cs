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

import { useEffect, useState } from 'react'
import { serversApi } from '../api'
import { useAppStore } from '../store'
import type { VirtualServerConfig } from '../types'
import styles from './ServerPanel.module.css'

const DEFAULT_PORT = 64738

function suggestPorts(servers: VirtualServerConfig[]): number[] {
  const maxPort = servers.length > 0 ? Math.max(...servers.map((s) => s.port)) : DEFAULT_PORT - 1
  const base = maxPort + 1
  return Array.from({ length: 5 }, (_, i) => base + i)
}

export default function ServerPanel() {
  const { hosts, selectedHostId, selectedServerId, selectServer, appendStatus } = useAppStore()
  const [servers,   setServers]   = useState<VirtualServerConfig[]>([])
  const [loading,   setLoading]   = useState(false)
  const [creating,  setCreating]  = useState(false)
  const [removing,  setRemoving]  = useState(false)
  const [toggling,  setToggling]  = useState<number | null>(null)
  const [newPort,   setNewPort]   = useState<number>(DEFAULT_PORT)

  const host      = hosts.find((h) => h.id === selectedHostId)
  const connected = host?.isConnected ?? false

  useEffect(() => {
    setServers([])
    selectServer(null)

    if (!selectedHostId || !connected) return

    setLoading(true)
    serversApi.list(selectedHostId)
      .then((list) => {
        setServers(list)
        const ports = suggestPorts(list)
        setNewPort(ports[0])
      })
      .catch(console.error)
      .finally(() => setLoading(false))
  }, [selectedHostId, connected])

  // Keep port suggestion in sync when server list changes
  useEffect(() => {
    if (servers.length > 0) {
      const ports = suggestPorts(servers)
      setNewPort((prev) => (ports.includes(prev) ? prev : ports[0]))
    }
  }, [servers])

  async function handleToggleRunning(s: VirtualServerConfig, e: React.MouseEvent) {
    e.stopPropagation()
    if (!selectedHostId || toggling !== null) return
    setToggling(s.serverId)
    try {
      if (s.isRunning) {
        await serversApi.stop(selectedHostId, s.serverId)
        appendStatus(`Server :${s.port} stopped.`)
      } else {
        await serversApi.start(selectedHostId, s.serverId)
        appendStatus(`Server :${s.port} started.`)
      }
      const updated = await serversApi.list(selectedHostId)
      setServers(updated)
    } catch (e: any) {
      appendStatus(`⚠ ${e.message}`)
    } finally {
      setToggling(null)
    }
  }

  async function handleCreate() {
    if (!selectedHostId || !connected) return
    setCreating(true)
    try {
      await serversApi.create(selectedHostId, newPort)
      appendStatus(`Server on :${newPort} created.`)
      const ufw = await serversApi.ufwOpen(selectedHostId, 0, newPort)
      if (ufw.ufwActive) appendStatus(`UFW: opened port ${newPort} — ${ufw.output?.trim()}`)
      const updated = await serversApi.list(selectedHostId)
      setServers(updated)
      const ports = suggestPorts(updated)
      setNewPort(ports[0])
    } catch (e: any) {
      appendStatus(`⚠ ${e.message}`)
    } finally {
      setCreating(false)
    }
  }

  async function handleRemove() {
    if (!selectedHostId || !selectedServerId) return
    const s = servers.find((sv) => sv.serverId === selectedServerId)
    if (!s) return
    if (!confirm(`Remove virtual server on :${s.port}? This cannot be undone.`)) return
    setRemoving(true)
    try {
      await serversApi.remove(selectedHostId, selectedServerId)
      appendStatus(`Server :${s.port} removed.`)
      selectServer(null)
      const updated = await serversApi.list(selectedHostId)
      setServers(updated)
      const ports = suggestPorts(updated)
      setNewPort(ports[0])
    } catch (e: any) {
      appendStatus(`⚠ ${e.message}`)
    } finally {
      setRemoving(false)
    }
  }

  const portOptions = suggestPorts(servers)

  if (!selectedHostId) {
    return (
      <div className={styles.panel}>
        <div className="section-header">Virtual Servers</div>
        <div className={styles.empty}>Select a host</div>
      </div>
    )
  }

  return (
    <div className={styles.panel}>
      <div className="section-header">
        Virtual Servers
        {host && <span className={styles.version}> — {host.displayName}</span>}
      </div>

      <ul className={styles.list}>
        {loading ? (
          <li className={styles.spinnerRow}>
            <div className={styles.spinner} />
            <span>Loading servers…</span>
          </li>
        ) : servers.length === 0 ? (
          <li className={styles.empty}>
            {connected ? 'No virtual servers' : 'Connect to host first'}
          </li>
        ) : (
          servers.map((s) => (
            <li
              key={s.serverId}
              className={`${styles.item} ${selectedServerId === s.serverId ? styles.selected : ''}`}
              onClick={() => selectServer(s.serverId)}
            >
              <span className={styles.port}>:{s.port}</span>
              <span className={styles.info}>
                <span className={styles.name}>{s.serverName || '(unnamed)'}</span>
                <span className={styles.slots}>{s.slots} slots</span>
              </span>
              <button
                className={`${styles.statusBtn} ${s.isRunning ? styles.running : styles.stopped}`}
                title={s.isRunning ? 'Click to stop' : 'Click to start'}
                disabled={toggling === s.serverId}
                onClick={(e) => handleToggleRunning(s, e)}
              >
                {toggling === s.serverId
                  ? <span className={styles.btnSpinner} />
                  : s.isRunning ? '▶ RUNNING' : '■ STOPPED'}
              </button>
            </li>
          ))
        )}
      </ul>

      {connected && (
        <div className={styles.footer}>
          <button
            className="btn-danger"
            disabled={!selectedServerId || removing}
            onClick={handleRemove}
          >
            {removing ? '…' : 'Remove'}
          </button>
          <div className={styles.newRow}>
            <select
              className={styles.portSelect}
              value={newPort}
              onChange={(e) => setNewPort(Number(e.target.value))}
            >
              {portOptions.map((p) => (
                <option key={p} value={p}>:{p}</option>
              ))}
            </select>
            <button
              className="btn-accent"
              disabled={creating}
              onClick={handleCreate}
            >
              {creating ? '…' : '+ New Server'}
            </button>
          </div>
        </div>
      )}
    </div>
  )
}

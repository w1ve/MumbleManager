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

import { useEffect, useState, useCallback } from 'react'
import { serversApi } from '../api'
import { useAppStore } from '../store'
import type { VirtualServerConfig } from '../types'
import styles from './ConfigPanel.module.css'

export default function ConfigPanel() {
  const { selectedHostId, selectedServerId, hosts, appendStatus } = useAppStore()
  const host = hosts.find((h) => h.id === selectedHostId)

  const [cfg,     setCfg]     = useState<VirtualServerConfig | null>(null)
  const [dirty,   setDirty]   = useState(false)
  const [saving,  setSaving]  = useState(false)
  const [loading, setLoading] = useState(false)

  const load = useCallback(async () => {
    if (!selectedHostId || !selectedServerId) { setCfg(null); return }
    setLoading(true)
    setCfg(null)
    try {
      const c = await serversApi.getConfig(selectedHostId, selectedServerId)
      setCfg(c)
      setDirty(false)
    } catch (e: any) {
      appendStatus(`⚠ ${e.message}`)
    } finally {
      setLoading(false)
    }
  }, [selectedHostId, selectedServerId])

  useEffect(() => { load() }, [load])

  function patch<K extends keyof VirtualServerConfig>(key: K, value: VirtualServerConfig[K]) {
    setCfg((c) => c ? { ...c, [key]: value } : c)
    setDirty(true)
  }

  async function handleSave() {
    if (!cfg || !selectedHostId || !selectedServerId) return
    setSaving(true)
    try {
      await serversApi.saveConfig(selectedHostId, selectedServerId, cfg)
      setDirty(false)
      appendStatus('✓ Configuration saved.')
    } catch (e: any) {
      appendStatus(`⚠ Save failed: ${e.message}`)
    } finally {
      setSaving(false)
    }
  }

  if (!selectedHostId || !selectedServerId) {
    return <div className={styles.empty}>Select a virtual server to configure it</div>
  }

  if (loading) {
    return (
      <div className={styles.empty}>
        <div className={styles.spinner} />
        <div style={{ marginTop: 12, color: 'var(--text-dim)', fontSize: 12 }}>
          Loading configuration…
        </div>
      </div>
    )
  }

  if (!cfg) return null

  const connected = host?.isConnected ?? false

  return (
    <div className={styles.panel}>
      <div className={styles.header}>
        <span>
          :{cfg.port} — {cfg.serverName || '(unnamed)'}
          <span className={cfg.isRunning ? styles.running : styles.stopped}>
            {cfg.isRunning ? '  ▶ RUNNING' : '  ■ STOPPED'}
          </span>
        </span>
        <button
          className="btn-accent"
          disabled={!dirty || saving || !connected}
          onClick={handleSave}
        >
          {saving ? 'Saving…' : 'Save Changes'}
        </button>
      </div>

      <div className={styles.form}>
        <Row label="Server Name">
          <input value={cfg.serverName} disabled={!connected}
            onChange={(e) => patch('serverName', e.target.value)} />
        </Row>
        <Row label="Port">
          <input type="number" value={cfg.port} disabled={!connected}
            onChange={(e) => patch('port', parseInt(e.target.value) || cfg.port)} />
        </Row>
        <Row label="Password">
          <input type="password" value={cfg.serverPassword} disabled={!connected}
            placeholder="(no password)"
            onChange={(e) => patch('serverPassword', e.target.value)} />
        </Row>
        <Row label="Default Channel">
          <input value={cfg.defaultChannel} disabled={!connected}
            placeholder="0"
            onChange={(e) => patch('defaultChannel', e.target.value)} />
        </Row>
        <Row label="Max Slots">
          <input type="number" min={1} max={10000} value={cfg.slots} disabled={!connected}
            onChange={(e) => patch('slots', parseInt(e.target.value) || cfg.slots)} />
        </Row>
        <Row label="User Timeout (s)">
          <input type="number" min={1} value={cfg.userTimeout} disabled={!connected}
            onChange={(e) => patch('userTimeout', parseInt(e.target.value) || cfg.userTimeout)} />
        </Row>
        <Row label="Max Message Length">
          <input type="number" min={1} value={cfg.maxMessageLength} disabled={!connected}
            onChange={(e) => patch('maxMessageLength', parseInt(e.target.value) || cfg.maxMessageLength)} />
        </Row>
        <Row label={`Bandwidth: ${formatBandwidth(cfg.userBandwidth)}`}>
          <input type="range" min={8000} max={320000} step={1000}
            value={cfg.userBandwidth} disabled={!connected}
            className={styles.slider}
            onChange={(e) => patch('userBandwidth', parseInt(e.target.value))} />
        </Row>
        <Row label="Welcome Message">
          <textarea rows={4} value={cfg.welcomeMessage} disabled={!connected}
            onChange={(e) => patch('welcomeMessage', e.target.value)} />
        </Row>
        <div className={styles.checks}>
          <Check label="Allow HTML"       checked={cfg.allowHtml}       disabled={!connected} onChange={(v) => patch('allowHtml', v)} />
          <Check label="Allow Ping"       checked={cfg.allowPing}       disabled={!connected} onChange={(v) => patch('allowPing', v)} />
          <Check label="Remember Channel" checked={cfg.rememberChannel} disabled={!connected} onChange={(v) => patch('rememberChannel', v)} />
        </div>
      </div>
    </div>
  )
}

function Row({ label, children }: { label: string; children: React.ReactNode }) {
  return (
    <div className={styles.row}>
      <label>{label}</label>
      {children}
    </div>
  )
}

function Check({ label, checked, disabled, onChange }: {
  label: string; checked: boolean; disabled: boolean; onChange: (v: boolean) => void
}) {
  return (
    <label className={styles.checkLabel}>
      <input type="checkbox" checked={checked} disabled={disabled}
        onChange={(e) => onChange(e.target.checked)} />
      {label}
    </label>
  )
}

function formatBandwidth(bps: number) {
  if (bps >= 1000) return `${(bps / 1000).toFixed(0)} kbps`
  return `${bps} bps`
}

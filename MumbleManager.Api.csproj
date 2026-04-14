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

import { useState } from 'react'
import { hostsApi } from '../api'
import type { SshHostEntry } from '../types'
import styles from './Dialog.module.css'

interface Props {
  existing: SshHostEntry | null
  onClose:  () => void
  onSaved:  () => void
}

export default function HostDialog({ existing, onClose, onSaved }: Props) {
  const [form, setForm] = useState({
    displayName: existing?.displayName ?? '',
    host:        existing?.host        ?? '',
    sshPort:     existing?.sshPort     ?? 22,
    username:    existing?.username    ?? '',
    password:    '',
    iceSecret:   existing?.iceSecret   ?? '',
  })
  const [saving, setSaving] = useState(false)
  const [error,  setError]  = useState<string | null>(null)

  const set = (k: string, v: string | number) => setForm((f) => ({ ...f, [k]: v }))

  async function handleSave() {
    if (!form.displayName || !form.host || !form.username) {
      setError('Display name, host and username are required.')
      return
    }
    setSaving(true)
    setError(null)
    try {
      if (existing) {
        await hostsApi.update(existing.id, form)
      } else {
        if (!form.password) { setError('Password is required for new hosts.'); setSaving(false); return }
        await hostsApi.create(form)
      }
      onSaved()
    } catch (e: any) {
      setError(e.message)
      setSaving(false)
    }
  }

  return (
    <div className={styles.overlay} onClick={onClose}>
      <div className={styles.dialog} onClick={(e) => e.stopPropagation()}>
        <div className={styles.title}>{existing ? 'Edit Host' : 'Add Host'}</div>

        <div className={styles.fields}>
          <Field label="Display Name">
            <input value={form.displayName} onChange={(e) => set('displayName', e.target.value)} />
          </Field>
          <Field label="Host / IP">
            <input value={form.host} onChange={(e) => set('host', e.target.value)} />
          </Field>
          <Field label="SSH Port">
            <input type="number" value={form.sshPort}
              onChange={(e) => set('sshPort', parseInt(e.target.value) || 22)} />
          </Field>
          <Field label="Username">
            <input value={form.username} onChange={(e) => set('username', e.target.value)} />
          </Field>
          <Field label={existing ? 'Password (leave blank to keep)' : 'Password'}>
            <input type="password" value={form.password}
              onChange={(e) => set('password', e.target.value)} />
          </Field>
          <Field label="Ice Secret (optional)">
            <input type="password" value={form.iceSecret} onChange={(e) => set('iceSecret', e.target.value)} />
          </Field>
        </div>

        {error && <div className={styles.error}>{error}</div>}

        <div className={styles.buttons}>
          <button className="btn-ghost"  onClick={onClose}>Cancel</button>
          <button className="btn-accent" disabled={saving} onClick={handleSave}>
            {saving ? 'Saving…' : 'Save'}
          </button>
        </div>
      </div>
    </div>
  )
}

function Field({ label, children }: { label: string; children: React.ReactNode }) {
  return (
    <div style={{ display: 'flex', flexDirection: 'column', gap: 4 }}>
      <label>{label}</label>
      {children}
    </div>
  )
}

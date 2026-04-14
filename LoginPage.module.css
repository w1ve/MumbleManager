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
import { authApi } from '../api'
import dialogStyles from './Dialog.module.css'

interface Props { onClose: () => void }

export default function ChangePasswordDialog({ onClose }: Props) {
  const [current,  setCurrent]  = useState('')
  const [next,     setNext]     = useState('')
  const [confirm,  setConfirm]  = useState('')
  const [error,    setError]    = useState<string | null>(null)
  const [saving,   setSaving]   = useState(false)
  const [done,     setDone]     = useState(false)

  async function handleSave() {
    if (next !== confirm) { setError('New passwords do not match.'); return }
    if (next.length < 6)  { setError('Password must be at least 6 characters.'); return }
    setSaving(true); setError(null)
    try {
      await authApi.changePassword(current, next)
      setDone(true)
    } catch (e: any) {
      setError(e.message)
      setSaving(false)
    }
  }

  return (
    <div className={dialogStyles.overlay} onClick={onClose}>
      <div className={dialogStyles.dialog} onClick={(e) => e.stopPropagation()}>
        <div className={dialogStyles.title}>Change Password</div>

        {done ? (
          <div style={{ padding: '20px 16px', color: 'var(--accent)', fontSize: 13 }}>
            ✓ Password changed successfully.
            <div style={{ marginTop: 16 }}>
              <button className="btn-accent" onClick={onClose}>Close</button>
            </div>
          </div>
        ) : (
          <>
            <div className={dialogStyles.fields}>
              <Field label="Current Password">
                <input type="password" value={current}
                  onChange={(e) => setCurrent(e.target.value)} />
              </Field>
              <Field label="New Password">
                <input type="password" value={next}
                  onChange={(e) => setNext(e.target.value)} />
              </Field>
              <Field label="Confirm New Password">
                <input type="password" value={confirm}
                  onChange={(e) => setConfirm(e.target.value)} />
              </Field>
            </div>
            {error && <div className={dialogStyles.error}>{error}</div>}
            <div className={dialogStyles.buttons}>
              <button className="btn-ghost" onClick={onClose}>Cancel</button>
              <button className="btn-accent" disabled={saving || !current || !next || !confirm}
                onClick={handleSave}>
                {saving ? 'Saving…' : 'Change Password'}
              </button>
            </div>
          </>
        )}
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

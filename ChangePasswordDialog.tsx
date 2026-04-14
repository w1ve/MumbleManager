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
import { usersApi } from '../api'
import type { AppUser } from '../types'
import styles from './UserManagement.module.css'
import dialogStyles from './Dialog.module.css'

export default function UserManagement() {
  const [users,         setUsers]        = useState<AppUser[]>([])
  const [selected,      setSelected]     = useState<AppUser | null>(null)
  const [showForm,      setShowForm]     = useState(false)
  const [editing,       setEditing]      = useState<AppUser | null>(null)
  const [status,        setStatus]       = useState<string | null>(null)
  const [deleteEmail,   setDeleteEmail]  = useState('')
  const [deleting,      setDeleting]     = useState(false)

  const load = () => usersApi.list().then(setUsers).catch(console.error)
  useEffect(() => { load() }, [])

  function openCreate() { setEditing(null); setShowForm(true) }
  function openEdit(u: AppUser) { setEditing(u); setShowForm(true) }

  async function handleDelete(u: AppUser) {
    if (!confirm(`Delete user "${u.username}"? This removes all their hosts and templates.`)) return
    try {
      await usersApi.remove(u.id)
      setStatus(`✓ User "${u.username}" deleted.`)
      setSelected(null)
      load()
    } catch (e: any) { setStatus(`⚠ ${e.message}`) }
  }

  async function handleDeleteByEmail() {
    if (!deleteEmail.trim()) return
    if (!confirm(`Delete account with email "${deleteEmail}"?`)) return
    setDeleting(true)
    try {
      const res = await usersApi.deleteByEmail(deleteEmail.trim())
      setStatus(`✓ ${res.message}`)
      setDeleteEmail('')
      setSelected(null)
      load()
    } catch (e: any) {
      setStatus(`⚠ ${e.message}`)
    } finally {
      setDeleting(false)
    }
  }

  return (
    <div className={styles.panel}>
      <div className="section-header">User Management</div>

      <div className={styles.body}>
        {/* User list */}
        <div className={styles.list}>
          {users.map((u) => (
            <div
              key={u.id}
              className={`${styles.item} ${selected?.id === u.id ? styles.selected : ''}`}
              onClick={() => setSelected(u)}
            >
              <div className={styles.itemName}>
                {u.username}
                <span className={u.role === 'Admin' ? styles.badgeAdmin : styles.badgeUser}>
                  {u.role}
                </span>
              </div>
              <div className={styles.itemEmail}>{u.email}</div>
            </div>
          ))}
          {users.length === 0 && (
            <div className={styles.emptyList}>No users yet</div>
          )}
        </div>

        {/* Detail pane */}
        <div className={styles.detail}>
          {selected ? (
            <>
              <div className={styles.detailName}>{selected.username}</div>
              <div className={styles.detailEmail}>{selected.email}</div>
              <div className={styles.detailMeta}>
                Role: {selected.role} · Joined {new Date(selected.createdUtc).toLocaleDateString()}
              </div>
              <div className={styles.detailActions}>
                <button className="btn-dim" onClick={() => openEdit(selected)}>Edit</button>
                {selected.role !== 'Admin' && (
                  <button className="btn-danger" onClick={() => handleDelete(selected)}>
                    Delete
                  </button>
                )}
              </div>
            </>
          ) : (
            <div className={styles.empty}>Select a user to manage them</div>
          )}

          {/* Delete by email */}
          <div className={styles.deleteByEmail}>
            <div className={styles.deleteByEmailLabel}>Delete account by email address</div>
            <div className={styles.deleteByEmailRow}>
              <input
                type="email"
                value={deleteEmail}
                onChange={(e) => setDeleteEmail(e.target.value)}
                placeholder="user@example.com"
                onKeyDown={(e) => e.key === 'Enter' && handleDeleteByEmail()}
              />
              <button
                className="btn-danger"
                disabled={deleting || !deleteEmail.trim()}
                onClick={handleDeleteByEmail}
              >
                {deleting ? '…' : 'Delete'}
              </button>
            </div>
          </div>

          {status && (
            <div className={status.startsWith('⚠') ? styles.statusError : styles.status}>
              {status}
            </div>
          )}
        </div>
      </div>

      <div className={styles.footer}>
        <button className="btn-accent" onClick={openCreate}>+ New User</button>
      </div>

      {showForm && (
        <UserFormDialog
          existing={editing}
          onClose={() => setShowForm(false)}
          onSaved={() => { setShowForm(false); load(); setStatus('✓ Saved.') }}
        />
      )}
    </div>
  )
}

// ── User create/edit dialog ───────────────────────────────────────────────────

function UserFormDialog({
  existing, onClose, onSaved,
}: { existing: AppUser | null; onClose: () => void; onSaved: () => void }) {
  const [username, setUsername] = useState(existing?.username ?? '')
  const [email,    setEmail]    = useState(existing?.email    ?? '')
  const [password, setPassword] = useState('')
  const [role,     setRole]     = useState(existing?.role ?? 'User')
  const [resetPw,  setResetPw]  = useState('')
  const [error,    setError]    = useState<string | null>(null)
  const [saving,   setSaving]   = useState(false)

  async function handleSave() {
    setSaving(true); setError(null)
    try {
      if (existing) {
        await usersApi.update(existing.id, {
          username: username || undefined,
          email:    email    || undefined,
          role:     role     || undefined,
        })
        if (resetPw) await usersApi.resetPassword(existing.id, resetPw)
      } else {
        if (!password) { setError('Password is required.'); setSaving(false); return }
        await usersApi.create(username, email, password, role)
      }
      onSaved()
    } catch (e: any) {
      setError(e.message)
      setSaving(false)
    }
  }

  return (
    <div className={dialogStyles.overlay} onClick={onClose}>
      <div className={dialogStyles.dialog} onClick={(e) => e.stopPropagation()}>
        <div className={dialogStyles.title}>
          {existing ? `Edit — ${existing.username}` : 'New User'}
        </div>
        <div className={dialogStyles.fields}>
          <Field label="Username">
            <input value={username} onChange={(e) => setUsername(e.target.value)} />
          </Field>
          <Field label="Email">
            <input type="email" value={email} onChange={(e) => setEmail(e.target.value)} />
          </Field>
          <Field label="Role">
            <select value={role} onChange={(e) => setRole(e.target.value as any)}>
              <option value="User">User</option>
              <option value="Admin">Admin</option>
            </select>
          </Field>
          {!existing && (
            <Field label="Password">
              <input type="password" value={password}
                onChange={(e) => setPassword(e.target.value)} />
            </Field>
          )}
          {existing && (
            <Field label="Reset Password (leave blank to keep)">
              <input type="password" value={resetPw}
                onChange={(e) => setResetPw(e.target.value)}
                placeholder="New password…" />
            </Field>
          )}
        </div>
        {error && <div className={dialogStyles.error}>{error}</div>}
        <div className={dialogStyles.buttons}>
          <button className="btn-ghost" onClick={onClose}>Cancel</button>
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

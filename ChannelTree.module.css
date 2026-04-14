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

import { resetConnection } from '../hooks/useSignalR'
import { useState } from 'react'
import { authApi } from '../api'
import { useAuthStore } from '../store/auth'
import type { AuthUser } from '../types'
import styles from './LoginPage.module.css'

type Mode = 'login' | 'register'

export default function LoginPage() {
  const setAuth = useAuthStore((s) => s.setAuth)
  const [mode,     setMode]     = useState<Mode>('login')
  const [username, setUsername] = useState('')
  const [email,    setEmail]    = useState('')
  const [password, setPassword] = useState('')
  const [confirm,  setConfirm]  = useState('')
  const [error,    setError]    = useState<string | null>(null)
  const [success,  setSuccess]  = useState<string | null>(null)
  const [loading,  setLoading]  = useState(false)

  function switchMode(m: Mode) {
    setMode(m); setError(null); setSuccess(null)
    setUsername(''); setEmail(''); setPassword(''); setConfirm('')
  }

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault()
    setError(null); setSuccess(null)

    if (mode === 'register') {
      if (password !== confirm) { setError('Passwords do not match.'); return }
      if (password.length < 6)  { setError('Password must be at least 6 characters.'); return }
    }

    setLoading(true)
    try {
      if (mode === 'login') {
        const res = await authApi.login(username, password)
        const user: AuthUser = {
          id:       res.user.id,
          username: res.user.username,
          email:    res.user.email,
          role:     res.user.role as 'Admin' | 'User',
        }
        
	setAuth(res.token, user)
        resetConnection()
      } else {
        await authApi.register(username, email, password)
        setSuccess('Account created! Check your email, then sign in.')
        switchMode('login')
      }
    } catch (e: any) {
      // Try to parse a JSON error body
      try {
        const body = JSON.parse(e.message)
        setError(body.message ?? e.message)
      } catch {
        setError(mode === 'login'
          ? 'Invalid username or password.'
          : e.message)
      }
    } finally {
      setLoading(false)
    }
  }

  return (
    <div className={styles.page}>
      <form className={styles.card} onSubmit={handleSubmit}>
        <div className={styles.logo}>MumbleManager</div>
        <div className={styles.subtitle}>Murmur Server Administration</div>

        {/* Mode tabs */}
        <div className={styles.modeTabs}>
          <button type="button"
            className={`${styles.modeTab} ${mode === 'login' ? styles.modeTabActive : ''}`}
            onClick={() => switchMode('login')}>Sign In</button>
          <button type="button"
            className={`${styles.modeTab} ${mode === 'register' ? styles.modeTabActive : ''}`}
            onClick={() => switchMode('register')}>Create Account</button>
        </div>

        <div className={styles.fields}>
          <div className={styles.field}>
            <label>Username</label>
            <input autoFocus value={username}
              onChange={(e) => setUsername(e.target.value)}
              placeholder="username"
              autoComplete={mode === 'login' ? 'username' : 'new-password'} />
          </div>

          {mode === 'register' && (
            <div className={styles.field}>
              <label>Email Address</label>
              <input type="email" value={email}
                onChange={(e) => setEmail(e.target.value)}
                placeholder="you@example.com"
                autoComplete="email" />
            </div>
          )}

          <div className={styles.field}>
            <label>Password</label>
            <input type="password" value={password}
              onChange={(e) => setPassword(e.target.value)}
              placeholder="••••••••"
              autoComplete={mode === 'login' ? 'current-password' : 'new-password'} />
          </div>

          {mode === 'register' && (
            <div className={styles.field}>
              <label>Confirm Password</label>
              <input type="password" value={confirm}
                onChange={(e) => setConfirm(e.target.value)}
                placeholder="••••••••"
                autoComplete="new-password" />
            </div>
          )}
        </div>

        {error   && <div className={styles.error}>{error}</div>}
        {success && <div className={styles.success}>{success}</div>}

        <button
          type="submit"
          className={`btn-accent ${styles.submit}`}
          disabled={loading || !username || !password ||
            (mode === 'register' && (!email || !confirm))}
        >
          {loading
            ? (mode === 'login' ? 'Signing in…' : 'Creating…')
            : (mode === 'login' ? 'Sign In' : 'Create Account')}
        </button>
      </form>
    </div>
  )
}

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

import { create } from 'zustand'

export interface AuthUser {
  id:       string
  username: string
  email:    string
  role:     'Admin' | 'User'
}

interface AuthState {
  token: string | null
  user:  AuthUser | null
  setAuth:   (token: string, user: AuthUser) => void
  clearAuth: () => void
}

// Module-level — set synchronously before any React re-render
let _token: string | null = sessionStorage.getItem('mm_token')

export function getToken() { return _token }

export const useAuthStore = create<AuthState>((set) => ({
  token: _token,
  user:  _token ? JSON.parse(sessionStorage.getItem('mm_user') ?? 'null') : null,
  setAuth: (token, user) => {
    _token = token
    sessionStorage.setItem('mm_token', token)
    sessionStorage.setItem('mm_user', JSON.stringify(user))
    set({ token, user })
  },
  clearAuth: () => {
    _token = null
    sessionStorage.removeItem('mm_token')
    sessionStorage.removeItem('mm_user')
    set({ token: null, user: null })
  },
}))

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

import { getToken } from '../store/auth'
import type {
  SshHostEntry, SshHostForm,
  VirtualServerConfig, ChannelInfo,
  ChannelTemplate, ConnectResult, AppUser,
} from '../types'

const BASE = '/api'

async function req<T>(method: string, path: string, body?: unknown): Promise<T> {
  const token = getToken()
  const headers: Record<string, string> = {}
  if (body) headers['Content-Type'] = 'application/json'
  if (token) headers['Authorization'] = `Bearer ${token}`

  const res = await fetch(`${BASE}${path}`, {
    method,
    headers,
    body: body ? JSON.stringify(body) : undefined,
  })

  if (res.status === 401) {
    throw new Error('Session expired. Please log in again.')
  }
  if (!res.ok) {
    const text = await res.text().catch(() => res.statusText)
    throw new Error(text || `HTTP ${res.status}`)
  }

  const ct = res.headers.get('content-type') ?? ''
  if (ct.includes('application/json')) return res.json() as Promise<T>
  return undefined as unknown as T
}

// ── Auth ──────────────────────────────────────────────────────────────────────

export const authApi = {
  login: (usernameOrEmail: string, password: string) =>
    req<{ token: string; user: { id: string; username: string; email: string; role: string } }>(
      'POST', '/auth/login', { usernameOrEmail, password }),

  me: () =>
    req<{ id: string; username: string; email: string; role: string }>('GET', '/auth/me'),

  register: (username: string, email: string, password: string) =>
    req<{ message: string }>('POST', '/auth/register', { username, email, password }),

  changePassword: (currentPassword: string, newPassword: string) =>
    req<void>('POST', '/auth/change-password', { currentPassword, newPassword }),
}

// ── Users (admin) ─────────────────────────────────────────────────────────────

export const usersApi = {
  list: () =>
    req<AppUser[]>('GET', '/users'),

  create: (username: string, email: string, password: string, role: string) =>
    req<{ id: string }>('POST', '/users', { username, email, password, role }),

  update: (id: string, data: { username?: string; email?: string; password?: string; role?: string }) =>
    req<void>('PUT', `/users/${id}`, data),

  remove: (id: string) =>
    req<void>('DELETE', `/users/${id}`),

  deleteByEmail: (emailAddress: string) =>
    req<{ message: string }>('DELETE', `/users/by-email/${encodeURIComponent(emailAddress)}`),

  resetPassword: (id: string, newPassword: string) =>
    req<void>('POST', `/users/${id}/reset-password`, { newPassword }),
}

// ── Hosts ─────────────────────────────────────────────────────────────────────

export const hostsApi = {
  list:          ()                              => req<SshHostEntry[]>('GET',    '/hosts'),
  create:        (form: SshHostForm)             => req<{ id: string }>('POST',   '/hosts', form),
  update:        (id: string, form: SshHostForm) => req<void>('PUT',  `/hosts/${id}`, form),
  remove:        (id: string)                    => req<void>('DELETE', `/hosts/${id}`),
  restartMumble: (id: string)                    => req<{ output: string }>('POST', `/hosts/${id}/restart-mumble`),
}

// ── Connection ────────────────────────────────────────────────────────────────

export const connectionApi = {
  connect:    (hostId: string) => req<ConnectResult>('POST',   `/hosts/${hostId}/connection`),
  disconnect: (hostId: string) => req<void>         ('DELETE', `/hosts/${hostId}/connection`),
  status:     (hostId: string) => req<{ connected: boolean; version: string | null }>(
                                    'GET', `/hosts/${hostId}/connection`),
}

// ── Servers ───────────────────────────────────────────────────────────────────

export const serversApi = {
  list:       (hostId: string) =>
    req<VirtualServerConfig[]>('GET', `/hosts/${hostId}/servers`),

  getConfig:  (hostId: string, serverId: number) =>
    req<VirtualServerConfig>('GET', `/hosts/${hostId}/servers/${serverId}/config`),

  saveConfig: (hostId: string, serverId: number, cfg: VirtualServerConfig) =>
    req<void>('PUT', `/hosts/${hostId}/servers/${serverId}/config`, cfg),

  create:     (hostId: string, port: number) =>
    req<{ serverId: number }>('POST', `/hosts/${hostId}/servers`, { port }),

  usedPorts:  (hostId: string) =>
    req<number[]>('GET', `/hosts/${hostId}/servers/used-ports`),

  ufwOpen:    (hostId: string, serverId: number, port: number) =>
    req<{ ufwActive: boolean; output: string | null }>(
      'POST', `/hosts/${hostId}/servers/${serverId}/ufw-open`, { port }),

  start:  (hostId: string, serverId: number) =>
    req<void>('POST', `/hosts/${hostId}/servers/${serverId}/start`),

  stop:   (hostId: string, serverId: number) =>
    req<void>('POST', `/hosts/${hostId}/servers/${serverId}/stop`),

  remove: (hostId: string, serverId: number) =>
    req<void>('DELETE', `/hosts/${hostId}/servers/${serverId}`),
}

// ── Channels ──────────────────────────────────────────────────────────────────

export const channelsApi = {
  list: (hostId: string, serverId: number) =>
    req<ChannelInfo[]>('GET', `/hosts/${hostId}/servers/${serverId}/channels`),

  add: (hostId: string, serverId: number, name: string, parentId: number) =>
    req<{ channelId: number }>('POST',
      `/hosts/${hostId}/servers/${serverId}/channels`, { name, parentId }),

  remove: (hostId: string, serverId: number, channelId: number) =>
    req<void>('DELETE', `/hosts/${hostId}/servers/${serverId}/channels/${channelId}`),

  rename: (hostId: string, serverId: number, channelId: number, name: string) =>
    req<void>('PATCH',
      `/hosts/${hostId}/servers/${serverId}/channels/${channelId}`, { name }),

  deleteAll: (hostId: string, serverId: number) =>
    req<void>('DELETE', `/hosts/${hostId}/servers/${serverId}/channels/all`),
}

// ── Templates ─────────────────────────────────────────────────────────────────

export const templatesApi = {
  list: () =>
    req<ChannelTemplate[]>('GET', '/templates'),

  create: (name: string, description: string, rootChildren: ChannelTemplate['rootChildren']) =>
    req<{ id: string }>('POST', '/templates', { name, description, rootChildren }),

  update: (id: string, name: string, description: string, rootChildren: ChannelTemplate['rootChildren']) =>
    req<void>('PUT', `/templates/${id}`, { name, description, rootChildren }),

  remove: (id: string) =>
    req<void>('DELETE', `/templates/${id}`),

  apply: (hostId: string, serverId: number, templateId: string) =>
    req<void>('POST', '/templates/apply', { hostId, serverId, templateId }),
}

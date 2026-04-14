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

// ── SSH Host ─────────────────────────────────────────────────────────────────

export interface SshHostEntry {
  id: string
  displayName: string
  host: string
  sshPort: number
  username: string
  iceSecret: string
  hasPassword: boolean
  isConnected: boolean
  cachedServers: VirtualServerConfig[]
}

export interface SshHostForm {
  displayName: string
  host: string
  sshPort: number
  username: string
  password: string
  iceSecret: string
}

// ── Virtual Server ───────────────────────────────────────────────────────────

export interface VirtualServerConfig {
  serverId: number
  port: number
  serverName: string
  serverPassword: string
  defaultChannel: string
  welcomeMessage: string
  allowHtml: boolean
  allowPing: boolean
  userBandwidth: number
  userTimeout: number
  maxMessageLength: number
  rememberChannel: boolean
  slots: number
  isRunning: boolean
}

// ── Channel ──────────────────────────────────────────────────────────────────

export interface ChannelInfo {
  id: number
  parentId: number
  name: string
  description: string
  position: number
  temporary: boolean
}

// ── Template ─────────────────────────────────────────────────────────────────

export interface ChannelTemplate {
  id: string
  name: string
  description: string
  createdUtc: string
  modifiedUtc: string
  rootChildren: ChannelNode[]
}

export interface ChannelNode {
  name: string
  description: string
  position: number
  temporary: boolean
  children: ChannelNode[]
}

// ── API Responses ─────────────────────────────────────────────────────────────

export interface ConnectResult {
  version: string
  serverCount: number
  servers: VirtualServerConfig[]
}

export interface ConnectionStatus {
  connected: boolean
  version: string | null
}

// ── Auth / Users ──────────────────────────────────────────────────────────────

export interface AuthUser {
  id:       string
  username: string
  email:    string
  role:     'Admin' | 'User'
}

export interface AppUser {
  id:         string
  username:   string
  email:      string
  role:       'Admin' | 'User'
  createdUtc: string
}

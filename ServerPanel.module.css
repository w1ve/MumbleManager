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
import type { SshHostEntry, VirtualServerConfig, ChannelTemplate } from '../types'

interface AppState {
  hosts:            SshHostEntry[]
  templates:        ChannelTemplate[]
  selectedHostId:   string | null
  selectedServerId: number | null
  statusLog:        string[]

  setHosts:         (hosts: SshHostEntry[]) => void
  updateHost:       (host: SshHostEntry) => void
  setTemplates:     (t: ChannelTemplate[]) => void
  selectHost:       (id: string | null) => void
  selectServer:     (id: number | null) => void
  appendStatus:     (msg: string) => void
  clearStatus:      () => void
  resetAll:         () => void   // ← clears everything on logout

  setHostConnected: (hostId: string, connected: boolean, servers?: VirtualServerConfig[]) => void
}

const initialState = {
  hosts:            [] as SshHostEntry[],
  templates:        [] as ChannelTemplate[],
  selectedHostId:   null as string | null,
  selectedServerId: null as number | null,
  statusLog:        [] as string[],
}

export const useAppStore = create<AppState>((set) => ({
  ...initialState,

  setHosts:     (hosts)     => set({ hosts }),
  setTemplates: (templates) => set({ templates }),

  updateHost: (host) =>
    set((s) => ({ hosts: s.hosts.map((h) => (h.id === host.id ? host : h)) })),

  selectHost: (id) =>
    set({ selectedHostId: id, selectedServerId: null }),

  selectServer: (id) => set({ selectedServerId: id }),

  appendStatus: (msg) =>
    set((s) => ({
      statusLog: [...s.statusLog.slice(-199), `${timestamp()} ${msg}`],
    })),

  clearStatus: () => set({ statusLog: [] }),

  // Called on logout — resets everything to initial state
  resetAll: () => set({ ...initialState }),

  setHostConnected: (hostId, connected, servers) =>
    set((s) => ({
      hosts: s.hosts.map((h) =>
        h.id === hostId
          ? { ...h, isConnected: connected, cachedServers: servers ?? h.cachedServers }
          : h
      ),
    })),
}))

function timestamp() {
  const d = new Date()
  return `[${d.getHours().toString().padStart(2,'0')}:${d.getMinutes().toString().padStart(2,'0')}:${d.getSeconds().toString().padStart(2,'0')}]`
}

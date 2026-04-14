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
import { channelsApi, templatesApi } from '../api'
import { useAppStore } from '../store'
import type { ChannelInfo, ChannelNode } from '../types'
import styles from './ChannelTree.module.css'

// ── Tree helpers ──────────────────────────────────────────────────────────────

interface TreeNode extends ChannelInfo {
  children: TreeNode[]
}

function buildTree(flat: ChannelInfo[]): TreeNode[] {
  const map = new Map<number, TreeNode>()
  flat.forEach((c) => map.set(c.id, { ...c, children: [] }))
  const roots: TreeNode[] = []
  flat.forEach((c) => {
    if (c.parentId === c.id || c.id === 0) {
      // Root channel
      roots.push(map.get(c.id)!)
    } else {
      const parent = map.get(c.parentId)
      if (parent) parent.children.push(map.get(c.id)!)
      else roots.push(map.get(c.id)!)
    }
  })
  // Sort by position
  const sort = (nodes: TreeNode[]) => {
    nodes.sort((a, b) => a.position - b.position)
    nodes.forEach((n) => sort(n.children))
  }
  sort(roots)
  return roots
}

// ── Component ─────────────────────────────────────────────────────────────────

export default function ChannelTree() {
  const { selectedHostId, selectedServerId, hosts, appendStatus } = useAppStore()
  const host = hosts.find((h) => h.id === selectedHostId)
  const connected = host?.isConnected ?? false

  const [channels,  setChannels]  = useState<ChannelInfo[]>([])
  const [loading,   setLoading]   = useState(false)
  const [selected,  setSelected]  = useState<number | null>(null)
  const [renaming,  setRenaming]  = useState<number | null>(null)
  const [renameVal, setRenameVal] = useState('')
  const [addingTo,  setAddingTo]  = useState<number | null>(null)
  const [addName,   setAddName]   = useState('')

  // Template save UI
  const [savingTpl,   setSavingTpl]   = useState(false)
  const [tplName,     setTplName]     = useState('')
  const [tplDesc,     setTplDesc]     = useState('')
  const [showSaveTpl, setShowSaveTpl] = useState(false)

  const reload = useCallback(async () => {
    if (!selectedHostId || !selectedServerId || !connected) { setChannels([]); return }
    setLoading(true)
    try {
      const ch = await channelsApi.list(selectedHostId, selectedServerId)
      setChannels(ch)
    } catch (e: any) {
      appendStatus(`⚠ Channels: ${e.message}`)
    } finally {
      setLoading(false)
    }
  }, [selectedHostId, selectedServerId, connected])

  useEffect(() => { reload() }, [reload])

  async function handleAdd(parentId: number) {
    if (!selectedHostId || !selectedServerId || !addName.trim()) return
    try {
      await channelsApi.add(selectedHostId, selectedServerId, addName.trim(), parentId)
      appendStatus(`✓ Channel "${addName}" created.`)
      setAddingTo(null)
      setAddName('')
      await reload()
    } catch (e: any) { appendStatus(`⚠ ${e.message}`) }
  }

  async function handleRename(id: number) {
    if (!selectedHostId || !selectedServerId || !renameVal.trim()) return
    try {
      await channelsApi.rename(selectedHostId, selectedServerId, id, renameVal.trim())
      setRenaming(null)
      await reload()
    } catch (e: any) { appendStatus(`⚠ ${e.message}`) }
  }

  async function handleDelete(id: number, name: string) {
    if (!selectedHostId || !selectedServerId) return
    if (!confirm(`Delete channel "${name}" and all its children?`)) return
    try {
      await channelsApi.remove(selectedHostId, selectedServerId, id)
      setSelected(null)
      await reload()
    } catch (e: any) { appendStatus(`⚠ ${e.message}`) }
  }

  async function handleDeleteAll() {
    if (!selectedHostId || !selectedServerId) return
    if (!confirm('Delete ALL channels (except Root)?')) return
    await channelsApi.deleteAll(selectedHostId, selectedServerId)
    setSelected(null)
    await reload()
  }

  async function handleSaveTemplate() {
    if (!tplName.trim()) return
    setSavingTpl(true)
    try {
      // Convert flat channel list to template tree (exclude root)
      const tree = buildTree(channels.filter((c) => c.id !== 0))
      const toNodes = (nodes: TreeNode[]): ChannelNode[] =>
        nodes.map((n) => ({
          name: n.name, description: n.description,
          position: n.position, temporary: n.temporary,
          children: toNodes(n.children),
        }))
      await templatesApi.create(tplName, tplDesc, toNodes(tree))
      appendStatus(`✓ Template "${tplName}" saved.`)
      setShowSaveTpl(false)
      setTplName('')
      setTplDesc('')
    } catch (e: any) {
      appendStatus(`⚠ ${e.message}`)
    } finally {
      setSavingTpl(false)
    }
  }

  if (!selectedHostId || !selectedServerId) {
    return <div className={styles.empty}>Select a virtual server to manage channels</div>
  }
  if (loading) return (
    <div className={styles.empty}>
      <div className={styles.spinner} />
      <div style={{ marginTop: 12, color: 'var(--text-dim)', fontSize: 12 }}>Loading channels…</div>
    </div>
  )

  const tree = buildTree(channels)

  return (
    <div className={styles.panel}>
      <div className={styles.toolbar}>
        <span className={styles.label}>Channel Tree</span>
        <div className={styles.toolbarBtns}>
          <button className="btn-ghost" disabled={!connected} onClick={reload}>↺ Refresh</button>
          <button className="btn-dim"   disabled={!connected} onClick={() => setShowSaveTpl(true)}>
            Save as Template
          </button>
          <button className="btn-danger" disabled={!connected || channels.length <= 1}
            onClick={handleDeleteAll}>
            Clear All
          </button>
        </div>
      </div>

      <div className={styles.tree}>
        {tree.map((node) => (
          <TreeNodeRow
            key={node.id}
            node={node}
            depth={0}
            selected={selected}
            renaming={renaming}
            renameVal={renameVal}
            addingTo={addingTo}
            addName={addName}
            connected={connected}
            onSelect={setSelected}
            onStartRename={(id, name) => { setRenaming(id); setRenameVal(name) }}
            onRename={handleRename}
            onCancelRename={() => setRenaming(null)}
            onSetRenameVal={setRenameVal}
            onStartAdd={(id) => { setAddingTo(id); setAddName('') }}
            onAdd={handleAdd}
            onCancelAdd={() => setAddingTo(null)}
            onSetAddName={setAddName}
            onDelete={handleDelete}
          />
        ))}
      </div>

      {/* Add to root */}
      {connected && (
        <div className={styles.rootAdd}>
          {addingTo === -1 ? (
            <InlineInput
              value={addName}
              placeholder="New top-level channel name"
              onChange={setAddName}
              onConfirm={() => handleAdd(0)}
              onCancel={() => setAddingTo(null)}
            />
          ) : (
            <button className="btn-accent" onClick={() => { setAddingTo(-1); setAddName('') }}>
              + Add Top-Level Channel
            </button>
          )}
        </div>
      )}

      {/* Save template modal */}
      {showSaveTpl && (
        <div className={styles.overlay} onClick={() => setShowSaveTpl(false)}>
          <div className={styles.tplDialog} onClick={(e) => e.stopPropagation()}>
            <div className={styles.tplTitle}>Save Channel Template</div>
            <div className={styles.tplFields}>
              <label>Template Name</label>
              <input value={tplName} onChange={(e) => setTplName(e.target.value)} />
              <label>Description (optional)</label>
              <input value={tplDesc} onChange={(e) => setTplDesc(e.target.value)} />
            </div>
            <div className={styles.tplBtns}>
              <button className="btn-ghost" onClick={() => setShowSaveTpl(false)}>Cancel</button>
              <button className="btn-accent" disabled={savingTpl || !tplName.trim()}
                onClick={handleSaveTemplate}>
                {savingTpl ? 'Saving…' : 'Save'}
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  )
}

// ── TreeNodeRow ───────────────────────────────────────────────────────────────

interface RowProps {
  node: TreeNode
  depth: number
  selected: number | null
  renaming: number | null
  renameVal: string
  addingTo: number | null
  addName: string
  connected: boolean
  onSelect:       (id: number) => void
  onStartRename:  (id: number, name: string) => void
  onRename:       (id: number) => void
  onCancelRename: () => void
  onSetRenameVal: (v: string) => void
  onStartAdd:     (id: number) => void
  onAdd:          (parentId: number) => void
  onCancelAdd:    () => void
  onSetAddName:   (v: string) => void
  onDelete:       (id: number, name: string) => void
}

function TreeNodeRow(p: RowProps) {
  const isRoot     = p.node.id === 0
  const isSelected = p.selected === p.node.id
  const isRenaming = p.renaming === p.node.id
  const isAddingHere = p.addingTo === p.node.id

  return (
    <div>
      <div
        className={`${styles.row} ${isSelected ? styles.rowSelected : ''}`}
        style={{ paddingLeft: 12 + p.depth * 18 }}
        onClick={() => p.onSelect(p.node.id)}
      >
        <span className={styles.icon}>{p.node.children.length ? '▾' : '·'}</span>

        {isRenaming ? (
          <InlineInput
            value={p.renameVal}
            placeholder="Channel name"
            onChange={p.onSetRenameVal}
            onConfirm={() => p.onRename(p.node.id)}
            onCancel={p.onCancelRename}
          />
        ) : (
          <>
            <span className={`${styles.nodeName} ${isRoot ? styles.rootName : ''}`}>
              {p.node.name}
              {p.node.temporary && <span className={styles.tempBadge}> temp</span>}
            </span>
            {isSelected && p.connected && !isRoot && (
              <span className={styles.rowActions} onClick={(e) => e.stopPropagation()}>
                <button className="btn-ghost" onClick={() => p.onStartRename(p.node.id, p.node.name)}>
                  ✎
                </button>
                <button className="btn-ghost" onClick={() => p.onStartAdd(p.node.id)}>
                  +sub
                </button>
                <button className="btn-danger" onClick={() => p.onDelete(p.node.id, p.node.name)}>
                  ✕
                </button>
              </span>
            )}
            {isSelected && p.connected && isRoot && (
              <span className={styles.rowActions} onClick={(e) => e.stopPropagation()}>
                <button className="btn-ghost" onClick={() => p.onStartAdd(p.node.id)}>
                  +sub
                </button>
              </span>
            )}
          </>
        )}
      </div>

      {isAddingHere && (
        <div style={{ paddingLeft: 12 + (p.depth + 1) * 18 }} className={styles.inlineAddRow}>
          <InlineInput
            value={p.addName}
            placeholder="New channel name"
            onChange={p.onSetAddName}
            onConfirm={() => p.onAdd(p.node.id)}
            onCancel={p.onCancelAdd}
          />
        </div>
      )}

      {p.node.children.map((child) => (
        <TreeNodeRow key={child.id} {...p} node={child} depth={p.depth + 1} />
      ))}
    </div>
  )
}

// ── InlineInput ───────────────────────────────────────────────────────────────

function InlineInput({
  value, placeholder, onChange, onConfirm, onCancel,
}: {
  value: string; placeholder: string
  onChange: (v: string) => void
  onConfirm: () => void; onCancel: () => void
}) {
  return (
    <span className={styles.inlineInput} onClick={(e) => e.stopPropagation()}>
      <input
        autoFocus
        value={value}
        placeholder={placeholder}
        onChange={(e) => onChange(e.target.value)}
        onKeyDown={(e) => {
          if (e.key === 'Enter')  onConfirm()
          if (e.key === 'Escape') onCancel()
        }}
      />
      <button className="btn-accent" onClick={onConfirm}>✓</button>
      <button className="btn-ghost"  onClick={onCancel}>✕</button>
    </span>
  )
}

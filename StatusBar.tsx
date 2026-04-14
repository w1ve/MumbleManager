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
import { templatesApi } from '../api'
import { useAppStore } from '../store'
import type { ChannelTemplate } from '../types'
import styles from './TemplatePanel.module.css'

export default function TemplatePanel() {
  const { selectedHostId, selectedServerId, hosts, appendStatus } = useAppStore()
  const host = hosts.find((h) => h.id === selectedHostId)
  const connected = host?.isConnected ?? false

  const [templates, setTemplates] = useState<ChannelTemplate[]>([])
  const [selected,  setSelected]  = useState<string | null>(null)
  const [applying,  setApplying]  = useState(false)
  const [deleting,  setDeleting]  = useState<string | null>(null)

  useEffect(() => {
    templatesApi.list().then(setTemplates).catch(console.error)
  }, [])

  async function handleApply() {
    if (!selectedHostId || !selectedServerId || !selected) return
    if (!confirm('This will delete all current channels and replace them with the template. Continue?')) return
    setApplying(true)
    appendStatus('Applying template…')
    try {
      await templatesApi.apply(selectedHostId, selectedServerId, selected)
      appendStatus('✓ Template applied.')
    } catch (e: any) {
      appendStatus(`⚠ Apply failed: ${e.message}`)
    } finally {
      setApplying(false)
    }
  }

  async function handleDelete(id: string, name: string) {
    if (!confirm(`Delete template "${name}"?`)) return
    setDeleting(id)
    try {
      await templatesApi.remove(id)
      setTemplates((t) => t.filter((x) => x.id !== id))
      if (selected === id) setSelected(null)
    } catch (e: any) {
      appendStatus(`⚠ ${e.message}`)
    } finally {
      setDeleting(null)
    }
  }

  const tpl = templates.find((t) => t.id === selected)

  return (
    <div className={styles.panel}>
      <div className="section-header">Channel Templates</div>

      <div className={styles.body}>
        {/* Template list */}
        <div className={styles.list}>
          {templates.length === 0 && (
            <div className={styles.empty}>
              No templates saved yet.<br />
              Use the Channel Tree to save a template.
            </div>
          )}
          {templates.map((t) => (
            <div
              key={t.id}
              className={`${styles.item} ${selected === t.id ? styles.itemSelected : ''}`}
              onClick={() => setSelected(t.id)}
            >
              <div className={styles.itemName}>{t.name}</div>
              {t.description && (
                <div className={styles.itemDesc}>{t.description}</div>
              )}
              <div className={styles.itemMeta}>
                {countNodes(t.rootChildren)} channels ·{' '}
                {new Date(t.modifiedUtc).toLocaleDateString()}
              </div>
            </div>
          ))}
        </div>

        {/* Detail / actions pane */}
        {tpl && (
          <div className={styles.detail}>
            <div className={styles.detailName}>{tpl.name}</div>
            {tpl.description && (
              <div className={styles.detailDesc}>{tpl.description}</div>
            )}

            {/* Tree preview */}
            <div className={styles.preview}>
              <div className={styles.previewRoot}>Root</div>
              <NodePreview nodes={tpl.rootChildren} depth={1} />
            </div>

            <div className={styles.detailActions}>
              <button
                className="btn-accent"
                disabled={!connected || !selectedServerId || applying}
                onClick={handleApply}
              >
                {applying ? 'Applying…' : '⇒ Apply to Selected Server'}
              </button>
              <button
                className="btn-danger"
                disabled={deleting === tpl.id}
                onClick={() => handleDelete(tpl.id, tpl.name)}
              >
                {deleting === tpl.id ? '…' : 'Delete Template'}
              </button>
            </div>
            {(!connected || !selectedServerId) && (
              <div className={styles.hint}>
                Connect to a host and select a server to apply this template.
              </div>
            )}
          </div>
        )}
      </div>
    </div>
  )
}

function NodePreview({ nodes, depth }: { nodes: ChannelTemplate['rootChildren'], depth: number }) {
  return (
    <>
      {nodes.map((n, i) => (
        <div key={i}>
          <div className={styles.previewNode} style={{ paddingLeft: depth * 14 }}>
            <span className={styles.previewIcon}>
              {n.children.length > 0 ? '▾' : '·'}
            </span>
            {n.name}
            {n.temporary && <span className={styles.tempBadge}> temp</span>}
          </div>
          {n.children.length > 0 && (
            <NodePreview nodes={n.children} depth={depth + 1} />
          )}
        </div>
      ))}
    </>
  )
}

function countNodes(nodes: ChannelTemplate['rootChildren']): number {
  return nodes.reduce((acc, n) => acc + 1 + countNodes(n.children), 0)
}

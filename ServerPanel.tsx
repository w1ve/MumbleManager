/* =============================================================================
   MumbleManager
   Author:  Gerald Hull, W1VE
   Date:    April 14, 2026
   License: MIT License
   ============================================================================= */

.panel {
  display: flex;
  flex-direction: column;
  height: 100%;
  border-right: 1px solid var(--border);
  min-width: 220px;
  width: 220px;
}

@media (max-width: 700px) {
  .panel {
    width: 100%;
    min-width: unset;
    height: auto;
    max-height: 300px;
    border-right: none;
    border-bottom: 1px solid var(--border);
    overflow: hidden;
  }
}

.list {
  flex: 1;
  overflow-y: auto;
  list-style: none;
  min-height: 0;
}

.item {
  display: flex;
  align-items: center;
  gap: 10px;
  padding: 10px 12px;
  border-bottom: 1px solid var(--border);
  cursor: pointer;
}
.item:hover    { background: var(--panel); }
.selected      { background: var(--panel); }

.info {
  display: flex;
  flex-direction: column;
  min-width: 0;
}
.name {
  color: var(--text);
  font-size: 12px;
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}
.addr {
  color: var(--text-dim);
  font-size: 10px;
}

.empty {
  padding: 16px 12px;
  color: var(--text-dim);
  font-size: 11px;
}

.actions {
  display: flex;
  flex-direction: column;
  gap: 4px;
  padding: 8px;
  border-top: 1px solid var(--border);
  background: var(--panel);
  flex-shrink: 0;
}
.actions button { width: 100%; }

.footer {
  display: flex;
  flex-direction: column;
  gap: 4px;
  padding: 8px;
  border-top: 1px solid var(--border);
  flex-shrink: 0;
}
.footer button { width: 100%; }

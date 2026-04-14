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
    max-height: 220px;
    border-right: none;
    border-bottom: 1px solid var(--border);
  }
}

.list {
  flex: 1;
  overflow-y: auto;
  list-style: none;
}

.item {
  display: flex;
  align-items: center;
  gap: 8px;
  padding: 10px 12px;
  border-bottom: 1px solid var(--border);
  cursor: pointer;
}
.item:hover { background: var(--panel); }
.selected   { background: var(--panel); }

.port {
  color: var(--accent);
  font-size: 13px;
  font-weight: 600;
  min-width: 52px;
}

.info {
  display: flex;
  flex-direction: column;
  flex: 1;
  min-width: 0;
}

.name {
  color: var(--text);
  font-size: 11px;
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}

.slots {
  color: var(--text-dim);
  font-size: 10px;
}

.statusBtn {
  font-size: 9px;
  white-space: nowrap;
  background: transparent;
  border: 1px solid transparent;
  border-radius: 3px;
  padding: 2px 5px;
  cursor: pointer;
  transition: background .15s, border-color .15s;
  flex-shrink: 0;
}
.statusBtn:hover:not(:disabled) {
  border-color: currentColor;
  background: rgba(255,255,255,.06);
}
.statusBtn:disabled { opacity: .5; cursor: default; }
.running { color: var(--accent); }
.stopped { color: var(--red); }

.empty {
  padding: 16px 12px;
  color: var(--text-dim);
  font-size: 11px;
}

.version {
  color: var(--accent);
  font-size: 10px;
}

.footer {
  display: flex;
  flex-direction: column;
  gap: 6px;
  padding: 8px;
  border-top: 1px solid var(--border);
  background: var(--panel);
}
.footer > button { width: 100%; }

.newRow {
  display: flex;
  gap: 6px;
}

.portSelect {
  flex: 1;
  min-width: 0;
  background: var(--bg);
  color: var(--text);
  border: 1px solid var(--border);
  border-radius: var(--radius);
  padding: 4px 6px;
  font-size: 12px;
  cursor: pointer;
}

.btnSpinner {
  display: inline-block;
  width: 10px;
  height: 10px;
  border: 2px solid currentColor;
  border-top-color: transparent;
  border-radius: 50%;
  animation: spin 0.7s linear infinite;
  opacity: 0.8;
}

.spinnerRow {
  display: flex;
  align-items: center;
  gap: 10px;
  padding: 16px 12px;
  color: var(--text-dim);
  font-size: 11px;
  list-style: none;
}

.spinner {
  width: 16px;
  height: 16px;
  border: 2px solid var(--border);
  border-top-color: var(--accent);
  border-radius: 50%;
  flex-shrink: 0;
  animation: spin 0.8s linear infinite;
}

@keyframes spin {
  to { transform: rotate(360deg); }
}

/* =============================================================================
   MumbleManager
   Author:  Gerald Hull, W1VE
   Date:    April 14, 2026
   License: MIT License
   ============================================================================= */

.root {
  display: flex;
  flex-direction: column;
  height: 100vh;
  overflow: hidden;
}

@media (max-width: 700px) {
  .root {
    height: auto;
    min-height: 100vh;
    overflow: visible;
  }
}

/* ── Title bar ─────────────────────────────────────────────────────────────── */

.titleBar {
  display: flex;
  align-items: center;
  gap: 14px;
  padding: 8px 16px;
  background: var(--bg-solid);
  flex-shrink: 0;
}

.title {
  font-size: 16px;
  font-weight: 600;
  color: var(--accent);
  letter-spacing: .04em;
}

.subtitle {
  font-size: 10px;
  color: var(--text-dim);
  letter-spacing: .06em;
  text-transform: uppercase;
}

.titleSep {
  height: 1px;
  background: var(--border);
  flex-shrink: 0;
}

/* ── User area ─────────────────────────────────────────────────────────────── */

.userArea {
  margin-left: auto;
  position: relative;
}

.userBtn {
  display: flex;
  align-items: center;
  gap: 7px;
  font-size: 12px;
  padding: 4px 10px;
}

.userDot {
  display: inline-block;
  width: 7px;
  height: 7px;
  border-radius: 50%;
  background: var(--accent);
  flex-shrink: 0;
}

.adminBadge {
  font-size: 9px;
  background: var(--accent-dim);
  color: #000;
  border-radius: 3px;
  padding: 1px 5px;
}

.userMenu {
  position: absolute;
  right: 0;
  top: calc(100% + 6px);
  background: var(--panel);
  border: 1px solid var(--border);
  border-radius: var(--radius);
  min-width: 160px;
  z-index: 50;
  overflow: hidden;
}

.menuItem {
  display: block;
  width: 100%;
  text-align: left;
  background: transparent;
  border: none;
  border-radius: 0;
  padding: 9px 14px;
  color: var(--text);
  font-size: 12px;
  cursor: pointer;
}
.menuItem:hover { background: rgba(255,255,255,.06); }

.menuItemDanger { color: var(--red); }

.menuSep {
  height: 1px;
  background: var(--border);
}

/* ── Main body ─────────────────────────────────────────────────────────────── */

.body {
  display: flex;
  flex: 1;
  overflow: hidden;
}

.right {
  display: flex;
  flex-direction: column;
  flex: 1;
  min-width: 0;
  overflow: hidden;
}

/* ── Tabs ──────────────────────────────────────────────────────────────────── */

.tabs {
  display: flex;
  background: var(--panel);
  border-bottom: 1px solid var(--border);
  flex-shrink: 0;
}

.tab {
  background: transparent;
  color: var(--text-dim);
  border: none;
  border-bottom: 2px solid transparent;
  border-radius: 0;
  padding: 9px 18px;
  font-size: 11px;
  letter-spacing: .05em;
  text-transform: uppercase;
  cursor: pointer;
  transition: color .15s, border-color .15s;
}
.tab:hover:not(:disabled) { color: var(--text); }
.tab:disabled { opacity: .3; cursor: not-allowed; }
.tabActive {
  color: var(--accent) !important;
  border-bottom-color: var(--accent);
}

.tabBody {
  flex: 1;
  overflow: hidden;
  display: flex;
  flex-direction: column;
}

/* ── Mobile layout ─────────────────────────────────────────────────────────── */

.bodyMobile {
  flex-direction: column;
  overflow-y: auto;
}

/* Make panels full-width and auto-height on mobile */
.bodyMobile > :global(.panel),
.bodyMobile > * > :global(.panel) {
  width: 100% !important;
  min-width: unset !important;
  height: auto;
  max-height: 220px;
  border-right: none;
  border-bottom: 1px solid var(--border);
}

.mobileStack {
  display: flex;
  flex-direction: column;
  flex: 1;
  overflow-y: auto;
}

.mobileSection {
  border-bottom: 1px solid var(--border);
}

.mobileSectionHeader {
  display: flex;
  align-items: center;
  justify-content: space-between;
  width: 100%;
  padding: 10px 16px;
  background: var(--panel);
  border: none;
  border-radius: 0;
  color: var(--text);
  font-size: 11px;
  font-weight: 600;
  letter-spacing: .05em;
  text-transform: uppercase;
  cursor: pointer;
  text-align: left;
}
.mobileSectionHeader:hover:not(:disabled) { background: var(--bg); }

.mobileSectionDisabled {
  opacity: .4;
  cursor: not-allowed;
}

.mobileSectionChevron {
  font-size: 10px;
  color: var(--text-dim);
}

.mobileSectionBody {
  overflow: hidden;
  max-height: 400px;
  overflow-y: auto;
}

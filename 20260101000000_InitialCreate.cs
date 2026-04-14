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
  overflow: hidden;
}

.header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 10px 16px;
  background: var(--panel);
  border-bottom: 1px solid var(--border);
  font-size: 12px;
  color: var(--text);
  gap: 12px;
  flex-shrink: 0;
}

.running { color: var(--accent); }
.stopped { color: var(--red); }

.form {
  flex: 1;
  overflow-y: auto;
  padding: 16px;
  display: flex;
  flex-direction: column;
  gap: 14px;
}

.row {
  display: flex;
  flex-direction: column;
  gap: 4px;
}

.slider {
  appearance: none;
  width: 100%;
  height: 4px;
  background: var(--border);
  border-radius: 2px;
  border: none;
  padding: 0;
  cursor: pointer;
}
.slider::-webkit-slider-thumb {
  appearance: none;
  width: 14px;
  height: 14px;
  border-radius: 50%;
  background: var(--accent);
  cursor: pointer;
}
.slider:disabled { opacity: .4; cursor: not-allowed; }

.checks {
  display: flex;
  flex-direction: column;
  gap: 8px;
}

.checkLabel {
  display: flex;
  align-items: center;
  gap: 8px;
  color: var(--text);
  font-size: 12px;
  cursor: pointer;
}
.checkLabel input {
  width: auto;
  accent-color: var(--accent);
}

.empty {
  display: flex;
  align-items: center;
  justify-content: center;
  height: 100%;
  color: var(--text-dim);
  font-size: 12px;
}

.spinner {
  width: 28px;
  height: 28px;
  border: 3px solid var(--border);
  border-top-color: var(--accent);
  border-radius: 50%;
  animation: spin 0.8s linear infinite;
}

@keyframes spin {
  to { transform: rotate(360deg); }
}

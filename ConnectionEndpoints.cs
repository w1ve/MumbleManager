/* =============================================================================
   MumbleManager
   Author:  Gerald Hull, W1VE
   Date:    April 14, 2026
   License: MIT License
   ============================================================================= */

*, *::before, *::after { box-sizing: border-box; margin: 0; padding: 0; }

:root {
  --bg:         #12141800;
  --bg-solid:   #121418;
  --panel:      #1a1d23;
  --border:     #303642;
  --accent:     #00bc8c;
  --accent-dim: #007860;
  --text:       #d2d7dc;
  --text-dim:   #6e7887;
  --red:        #d23c3c;
  --yellow:     #dcb432;
  --font:       'JetBrains Mono', 'Consolas', monospace;
  --radius:     4px;
}

html, body, #root {
  height: 100%;
  background: var(--bg-solid);
  color: var(--text);
  font-family: var(--font);
  font-size: 13px;
  overflow: hidden;
}

@media (max-width: 700px) {
  html, body, #root { overflow: auto; height: auto; min-height: 100%; }
}

button {
  font-family: var(--font);
  font-size: 12px;
  cursor: pointer;
  border-radius: var(--radius);
  border: 1px solid transparent;
  padding: 5px 12px;
  transition: opacity .15s;
}
button:disabled { opacity: .4; cursor: not-allowed; }

input, textarea, select {
  font-family: var(--font);
  font-size: 12px;
  background: var(--panel);
  color: var(--text);
  border: 1px solid var(--border);
  border-radius: var(--radius);
  padding: 5px 8px;
  outline: none;
  width: 100%;
}
input:focus, textarea:focus, select:focus {
  border-color: var(--accent);
}

label { color: var(--text-dim); font-size: 11px; }

::-webkit-scrollbar       { width: 6px; height: 6px; }
::-webkit-scrollbar-track { background: transparent; }
::-webkit-scrollbar-thumb { background: var(--border); border-radius: 3px; }

/* Utility classes */
.btn-accent  { background: var(--panel); color: var(--accent);  border-color: var(--accent); }
.btn-dim     { background: var(--panel); color: var(--accent-dim); border-color: var(--accent-dim); }
.btn-danger  { background: var(--panel); color: var(--red);     border-color: var(--red); }
.btn-ghost   { background: transparent; color: var(--text-dim); border-color: var(--border); }

.btn-accent:hover:not(:disabled)  { background: var(--accent);     color: #000; }
.btn-dim:hover:not(:disabled)     { background: var(--accent-dim); color: #000; }
.btn-danger:hover:not(:disabled)  { background: var(--red);        color: #fff; }
.btn-ghost:hover:not(:disabled)   { border-color: var(--text-dim); color: var(--text); }

.section-header {
  font-size: 10px;
  letter-spacing: .08em;
  text-transform: uppercase;
  color: var(--text-dim);
  padding: 8px 12px 6px;
  background: var(--panel);
  border-bottom: 1px solid var(--border);
}

.dot {
  display: inline-block;
  width: 8px;
  height: 8px;
  border-radius: 50%;
  flex-shrink: 0;
}
.dot--on  { background: var(--accent); }
.dot--off { background: var(--text-dim); }

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

import { useState, useEffect } from 'react'
import { useAuthStore } from './store/auth'
import { resetConnection } from './hooks/useSignalR'
import LoginPage            from './components/LoginPage'
import HostPanel            from './components/HostPanel'
import ServerPanel          from './components/ServerPanel'
import ConfigPanel          from './components/ConfigPanel'
import ChannelTree          from './components/ChannelTree'
import TemplatePanel        from './components/TemplatePanel'
import UserManagement       from './components/UserManagement'
import ChangePasswordDialog from './components/ChangePasswordDialog'
import StatusBar            from './components/StatusBar'
import { useAppStore }      from './store'
import styles from './App.module.css'

type RightTab = 'config' | 'channels' | 'templates' | 'users'

function useIsMobile(breakpoint = 700) {
  const [isMobile, setIsMobile] = useState(() => window.innerWidth < breakpoint)
  useEffect(() => {
    const mq = window.matchMedia(`(max-width: ${breakpoint}px)`)
    const handler = (e: MediaQueryListEvent) => setIsMobile(e.matches)
    mq.addEventListener('change', handler)
    return () => mq.removeEventListener('change', handler)
  }, [breakpoint])
  return isMobile
}

export default function App() {
  const { user, clearAuth } = useAuthStore()
  const { selectedServerId, resetAll } = useAppStore()
  const isMobile = useIsMobile()

  const [rightTab,   setRightTab]   = useState<RightTab>('config')
  const [showMenu,   setShowMenu]   = useState(false)
  const [showChgPw,  setShowChgPw]  = useState(false)

  // Show login page if not authenticated
  if (!user) return <LoginPage />

  function handleLogout() {
    resetAll()
    clearAuth()
    resetConnection()
    setShowMenu(false)
  }

  return (
    <div className={styles.root}>
      {/* ── Title bar ──────────────────────────────────────────────────── */}
      <div className={styles.titleBar}>
        <span className={styles.title}>MumbleManager</span>
        <span className={styles.subtitle}>Murmur Server Administration</span>

        <div className={styles.userArea}>
          <button
            className={`btn-ghost ${styles.userBtn}`}
            onClick={() => setShowMenu((v) => !v)}
          >
            <span className={styles.userDot} />
            {user.username}
            {user.role === 'Admin' && <span className={styles.adminBadge}>admin</span>}
            <span style={{ fontSize: 9, marginLeft: 4 }}>▾</span>
          </button>

          {showMenu && (
            <div className={styles.userMenu} onClick={() => setShowMenu(false)}>
              <button className={styles.menuItem}
                onClick={() => { setShowChgPw(true) }}>
                Change Password
              </button>
              <div className={styles.menuSep} />
              <button className={`${styles.menuItem} ${styles.menuItemDanger}`}
                onClick={handleLogout}>
                Sign Out
              </button>
            </div>
          )}
        </div>
      </div>
      <div className={styles.titleSep} />

      {/* ── Main body ──────────────────────────────────────────────────── */}
      <div className={`${styles.body} ${isMobile ? styles.bodyMobile : ''}`}>
        <HostPanel />
        <ServerPanel />

        {isMobile ? (
          <div className={styles.mobileStack}>
            <MobileSection title="Configuration">
              <ConfigPanel />
            </MobileSection>
            <MobileSection title="Channels" disabled={!selectedServerId}>
              <ChannelTree />
            </MobileSection>
            <MobileSection title="Templates">
              <TemplatePanel />
            </MobileSection>
            {user.role === 'Admin' && (
              <MobileSection title="Users">
                <UserManagement />
              </MobileSection>
            )}
          </div>
        ) : (
          <div className={styles.right}>
            <div className={styles.tabs}>
              <Tab active={rightTab === 'config'} onClick={() => setRightTab('config')}>
                Configuration
              </Tab>
              <Tab active={rightTab === 'channels'} onClick={() => setRightTab('channels')}
                disabled={!selectedServerId}>
                Channels
              </Tab>
              <Tab active={rightTab === 'templates'} onClick={() => setRightTab('templates')}>
                Templates
              </Tab>
              {user.role === 'Admin' && (
                <Tab active={rightTab === 'users'} onClick={() => setRightTab('users')}>
                  Users
                </Tab>
              )}
            </div>

            <div className={styles.tabBody}>
              {rightTab === 'config'    && <ConfigPanel />}
              {rightTab === 'channels'  && <ChannelTree />}
              {rightTab === 'templates' && <TemplatePanel />}
              {rightTab === 'users'     && user.role === 'Admin' && <UserManagement />}
            </div>
          </div>
        )}
      </div>

      <StatusBar />

      {showChgPw && <ChangePasswordDialog onClose={() => setShowChgPw(false)} />}
    </div>
  )
}

function Tab({
  active, disabled, onClick, children,
}: {
  active: boolean; disabled?: boolean; onClick: () => void; children: React.ReactNode
}) {
  return (
    <button
      className={`${styles.tab} ${active ? styles.tabActive : ''}`}
      disabled={disabled}
      onClick={onClick}
    >
      {children}
    </button>
  )
}

function MobileSection({
  title, disabled, children,
}: {
  title: string; disabled?: boolean; children: React.ReactNode
}) {
  const [open, setOpen] = useState(true)
  return (
    <div className={styles.mobileSection}>
      <button
        className={`${styles.mobileSectionHeader} ${disabled ? styles.mobileSectionDisabled : ''}`}
        onClick={() => !disabled && setOpen((v) => !v)}
        disabled={disabled}
      >
        <span>{title}</span>
        <span className={styles.mobileSectionChevron}>{open && !disabled ? '▴' : '▾'}</span>
      </button>
      {open && !disabled && (
        <div className={styles.mobileSectionBody}>
          {children}
        </div>
      )}
    </div>
  )
}

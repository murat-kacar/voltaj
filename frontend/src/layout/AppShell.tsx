import { useState } from 'react'
import { AppBar, Toolbar, Typography, Drawer, Badge, Box, ButtonBase, IconButton, BottomNavigation, BottomNavigationAction, useMediaQuery } from '@mui/material'
import type { Theme } from '@mui/material'
import MenuIcon from '@mui/icons-material/Menu'
import NotificationsIcon from '@mui/icons-material/Notifications'

import { useI18n } from '../i18n'
import { SideNav } from '../navigation/SideNav'
import { UserMenu } from '../navigation/UserMenu'
import type { NavGroup } from '../navigation/navModel'
import { useNavigate } from 'react-router-dom'
import { sessionRoles, type AuthResult } from '../api'

const drawerWidth = 240

interface AppShellProps {
  session: AuthResult
  activeView: string
  navGroups: NavGroup[]
  mobileNavItems: NavGroup[]
  activeGroupId: string | undefined
  pendingReminders: number
  mode: 'light' | 'dark'
  onToggleMode: () => void
  onLogout: () => void
  children: React.ReactNode
}

export function AppShell({ session, activeView, navGroups, mobileNavItems, activeGroupId, pendingReminders, mode, onToggleMode, onLogout, children }: AppShellProps) {
  const { lang, setLang, translate: t } = useI18n()
  const navigate = useNavigate()
  const navTo = (id: string) => navigate(id === 'dashboard' ? '/' : '/' + id)
  const isMobile = useMediaQuery((theme: Theme) => theme.breakpoints.down('sm'))
  const [mobileNavOpen, setMobileNavOpen] = useState(false)

  return (
    <Box sx={{ display: 'flex', height: '100vh', overflow: 'hidden' }}>
      {isMobile && (
        <AppBar position="fixed" sx={{ zIndex: (theme) => theme.zIndex.drawer + 1, bgcolor: 'background.paper', color: 'text.primary', borderBottom: 1, borderColor: 'divider', boxShadow: 'none' }}>
          <Toolbar>
            <IconButton edge="start" onClick={() => setMobileNavOpen(true)} aria-label={t('common:nav.openMenu')} data-testid="nav-open-menu">
              <MenuIcon />
            </IconButton>
            <Typography variant="h6" noWrap component="span" sx={{ fontWeight: 700, color: 'primary.main', ml: 1 }}>
              {t('common:brand.name')}
            </Typography>
            <Box sx={{ flexGrow: 1 }} />
            <IconButton onClick={() => navTo('reminders')} color={activeView === 'reminders' ? 'primary' : 'default'} size="small">
              <Badge badgeContent={pendingReminders} color="primary" max={99}>
                <NotificationsIcon />
              </Badge>
            </IconButton>
          </Toolbar>
        </AppBar>
      )}

      {!isMobile && (
        <Drawer
          variant="permanent"
          sx={{
            width: drawerWidth,
            flexShrink: 0,
            [`& .MuiDrawer-paper`]: { width: drawerWidth, boxSizing: 'border-box' },
          }}
        >
          <Box sx={{ p: 2, borderBottom: 1, borderColor: 'divider', display: 'flex', alignItems: 'center', minHeight: 64 }}>
            <ButtonBase onClick={() => navTo('dashboard')} sx={{ justifyContent: 'flex-start' }} data-testid="nav-home">
              <Typography variant="h6" noWrap component="span" sx={{ fontWeight: 800, color: 'primary.main', letterSpacing: '-0.5px' }}>
                {t('common:brand.name')}
              </Typography>
            </ButtonBase>
          </Box>
          <Box sx={{ flexGrow: 1, overflow: 'auto', py: 2 }}>
            <SideNav
              groups={navGroups}
              activeView={activeView}
              onNavigate={navTo}
              ariaLabel={t('common:nav.mainNavigation')}
            />
          </Box>
          <Box sx={{ p: 2, borderTop: 1, borderColor: 'divider', display: 'flex', flexDirection: 'column', gap: 1 }}>
            <UserMenu
              name={session.name}
              role={sessionRoles().includes('Admin') ? 'Yönetici' : 'Personel'}
              initials={session.name.split(' ').map((p) => p[0]).join('').slice(0, 2)}
              lang={lang}
              mode={mode}
              onNavigate={navTo}
              onToggleLang={() => setLang(lang === 'en' ? 'tr' : 'en')}
              onToggleMode={onToggleMode}
              onLogout={onLogout}
            />
          </Box>
        </Drawer>
      )}

      {isMobile && (
        <Drawer
          open={mobileNavOpen}
          onClose={() => setMobileNavOpen(false)}
          sx={{ [`& .MuiDrawer-paper`]: { width: drawerWidth, boxSizing: 'border-box' } }}
        >
          <Toolbar />
          <Box sx={{ overflow: 'auto' }}>
            <SideNav
              groups={navGroups}
              activeView={activeView}
              onNavigate={(id) => { navTo(id); setMobileNavOpen(false) }}
              ariaLabel={t('common:nav.mainNavigation')}
            />
          </Box>
        </Drawer>
      )}

      <Box component="main" sx={{ flexGrow: 1, bgcolor: 'background.default', p: 3, pt: { xs: 10, sm: 3 }, pb: { xs: 9, sm: 3 }, overflow: 'auto' }}>
        {children}
      </Box>

      {isMobile && (
        <BottomNavigation
          value={activeGroupId ?? false}
          onChange={(_, newValue: string) => {
            const group = navGroups.find((g) => g.id === newValue)
            navTo(group?.items ? group.items[0].id : newValue)
          }}
          showLabels
          sx={{ position: 'fixed', bottom: 0, left: 0, right: 0, zIndex: 1000, borderTop: 1, borderColor: 'divider' }}
        >
          {mobileNavItems.map((item) => (
            <BottomNavigationAction key={item.id} label={item.label} value={item.id} icon={item.icon} />
          ))}
        </BottomNavigation>
      )}
    </Box>
  )
}

import { useMemo, useState, useEffect } from 'react'
import { useLocation, useNavigate } from 'react-router-dom'
import { ThemeProvider } from '@mui/material/styles'
import CssBaseline from '@mui/material/CssBaseline'
import { Box, Typography, Button } from '@mui/material'
import DashboardIcon from '@mui/icons-material/Dashboard'
import PointOfSaleIcon from '@mui/icons-material/PointOfSale'
import PeopleIcon from '@mui/icons-material/People'
import MiscellaneousServicesIcon from '@mui/icons-material/MiscellaneousServices'
import InventoryIcon from '@mui/icons-material/Inventory'

import { getTheme } from './theme'
import './App.css'
import { AuthView } from './AuthView'
import { authApi, remindersApi } from './api'
import { findGroupId, type NavGroup } from './navigation/navModel'
import { TestDataGeneratorView } from './features/generator/TestDataGeneratorView'
import { EndpointTriggerView } from './features/generator/EndpointTriggerView'
import { useI18n } from './i18n'

import { AppShell } from './layout/AppShell'
import { AppRouter } from './navigation/AppRouter'
import { useSession } from './hooks/useSession'


function App() {
  const { translate: t } = useI18n()
  const navigate = useNavigate()
  const location = useLocation()

  const activeView = location.pathname === '/' ? 'dashboard' : location.pathname.split('/').filter(Boolean)[0]

  const prefersDarkMode = window.matchMedia && window.matchMedia('(prefers-color-scheme: dark)').matches
  const [mode, setMode] = useState<'light' | 'dark'>(prefersDarkMode ? 'dark' : 'light')
  const theme = useMemo(() => getTheme(mode), [mode])

  const { session, setSession } = useSession()
  const [pendingReminders, setPendingReminders] = useState(0)

  useEffect(() => {
    if (session) {
      remindersApi.list({ state: 'Pending', limit: 1 }).then((page) => setPendingReminders(page.total)).catch(() => {})
    }
  }, [session, location.pathname])

  const navGroups: NavGroup[] = [
    { id: 'pos', label: t('common:nav.pos', 'POS / Hızlı Satış'), icon: <PointOfSaleIcon /> },
    { id: 'customers', label: t('common:nav.customers', 'Müşteriler & Finans'), icon: <PeopleIcon /> },
    { id: 'services', label: t('common:nav.services', 'Hizmetler'), icon: <MiscellaneousServicesIcon /> },
    { id: 'goods-receipt', label: t('common:nav.inventory', 'Ürün Kabul'), icon: <InventoryIcon /> },
    { id: 'dashboard', label: t('common:nav.dashboard', 'Özet & Yönetim'), icon: <DashboardIcon /> },
  ]

  const mobileNavItems = navGroups.filter(g => ['services', 'goods-receipt', 'dashboard', 'pos'].includes(g.id))
  const comingSoon = new Set<string>()
  const activeGroupId = findGroupId(navGroups, activeView)

  if (!session && (location.pathname === '/test-data' || location.pathname === '/endpoint-trigger')) {
    return (
      <ThemeProvider theme={theme}>
        <CssBaseline />
        <Box sx={{ minHeight: '100vh', bgcolor: 'background.default', py: 2 }}>
          <Box sx={{ maxWidth: 1440, mx: 'auto', px: 3, mb: 2, display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
            <Typography variant="h6" sx={{ fontWeight: 700, color: 'primary.main' }}>
              {'⚡ Voltflow'}
            </Typography>
            <Box sx={{ display: 'flex', gap: 1 }}>
              <Button
                variant={location.pathname === '/test-data' ? 'contained' : 'outlined'}
                size="small"
                onClick={() => navigate('/test-data')}
               data-testid="button-5e64ff">
                {t('common:nav.testData')}
              </Button>
              <Button
                variant={location.pathname === '/endpoint-trigger' ? 'contained' : 'outlined'}
                size="small"
                color="secondary"
                onClick={() => navigate('/endpoint-trigger')}
               data-testid="button-4ccf51">
                {t('common:nav.endpointTrigger')}
              </Button>
              <Button variant="outlined" size="small" onClick={() => navigate('/login')} data-testid="button-a7c8f7">
                {t('common:auth.login')}
              </Button>
            </Box>
          </Box>
          {location.pathname === '/endpoint-trigger' ? <EndpointTriggerView /> : <TestDataGeneratorView />}
        </Box>
      </ThemeProvider>
    )
  }

  if (!session) return <AuthView onAuthenticated={setSession} />

  const handleLogout = async () => {
    if (session.token) {
      try { await authApi.revokeSession(session.token) } catch { /* ignore */ }
    }
    localStorage.removeItem('voltflow.session')
    setSession(null)
  }

  return (
    <ThemeProvider theme={theme}>
      <CssBaseline />
      <AppShell
        session={session}
        activeView={activeView}
        navGroups={navGroups}
        mobileNavItems={mobileNavItems}
        activeGroupId={activeGroupId}
        pendingReminders={pendingReminders}
        mode={mode}
        onToggleMode={() => setMode(mode === 'dark' ? 'light' : 'dark')}
        onLogout={handleLogout}
      >
        <AppRouter navGroups={navGroups} comingSoon={comingSoon} />
      </AppShell>
    </ThemeProvider>
  )
}

export default App

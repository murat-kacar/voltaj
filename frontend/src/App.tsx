import { useMemo, useState, useEffect } from 'react'
import { useLocation, useNavigate } from 'react-router-dom'
import { ThemeProvider } from '@mui/material/styles'
import CssBaseline from '@mui/material/CssBaseline'
import { Box, Typography, Button } from '@mui/material'
import AutoFixHighIcon from '@mui/icons-material/AutoFixHigh'
import HttpIcon from '@mui/icons-material/Http'
import MenuBookIcon from '@mui/icons-material/MenuBook'
import Inventory2Icon from '@mui/icons-material/Inventory2'
import BarChartIcon from '@mui/icons-material/BarChart'
import CalendarMonthIcon from '@mui/icons-material/CalendarMonth'
import EventRepeatIcon from '@mui/icons-material/EventRepeat'
import PeopleIcon from '@mui/icons-material/People'
import WorkIcon from '@mui/icons-material/Work'
import PaymentIcon from '@mui/icons-material/Payment'
import PointOfSaleIcon from '@mui/icons-material/PointOfSale'
import RequestQuoteIcon from '@mui/icons-material/RequestQuote'
import ReceiptLongIcon from '@mui/icons-material/ReceiptLong'
import FolderOpenIcon from '@mui/icons-material/FolderOpen'
import MoveToInboxIcon from '@mui/icons-material/MoveToInbox'
import ManageAccountsIcon from '@mui/icons-material/ManageAccounts'

import { getTheme } from './theme'
import './App.css'
import { AuthView } from './AuthView'
import { authApi, remindersApi, workOrdersApi } from './api'
import { findGroupId, type NavGroup } from './navigation/navModel'
import { TestDataGeneratorView } from './generator/TestDataGeneratorView'
import { EndpointTriggerView } from './generator/EndpointTriggerView'
import { useI18n } from './i18n'

import { AppShell } from './layout/AppShell'
import { AppRouter } from './navigation/AppRouter'
import { useSession } from './hooks/useSession'

const TERMINAL_STATUSES = new Set(['Invoiced', 'Cancelled', 'NoShow'])

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
  const [activeOrderCount, setActiveOrderCount] = useState(0)

  useEffect(() => {
    if (session) {
      remindersApi.list({ state: 'Pending', limit: 1 }).then((page) => setPendingReminders(page.total)).catch(() => {})
      workOrdersApi.list().then(page => {
        setActiveOrderCount(page.items.filter(o => !TERMINAL_STATUSES.has(o.status)).length)
      }).catch(() => {})
    }
  }, [session, location.pathname])

  const navGroups: NavGroup[] = [
    { id: 'test-data', label: t('common:nav.testData'), icon: <AutoFixHighIcon color="primary" /> },
    { id: 'endpoint-trigger', label: t('common:nav.endpointTrigger'), icon: <HttpIcon color="secondary" /> },
    { id: 'schedule', label: t('common:nav.calendar'), icon: <CalendarMonthIcon /> },
    { id: 'customers', label: t('common:nav.customers'), icon: <PeopleIcon /> },
    { id: 'quotes', label: t('common:nav.quotes'), icon: <RequestQuoteIcon /> },
    { id: 'work-orders', label: t('common:nav.workOrders'), icon: <WorkIcon />, badge: activeOrderCount > 0 ? activeOrderCount : undefined },
    { id: 'product-intake', label: t('common:nav.productIntake'), icon: <MoveToInboxIcon /> },
    { id: 'projects', label: t('common:nav.projects'), icon: <FolderOpenIcon /> },
    { id: 'invoices', label: t('common:nav.invoices'), icon: <ReceiptLongIcon /> },
    { id: 'payments', label: t('common:nav.payments'), icon: <PaymentIcon /> },
    { id: 'quick-sale', label: t('common:nav.quickSale'), icon: <PointOfSaleIcon /> },
    { id: 'catalog', label: t('common:nav.catalog'), icon: <MenuBookIcon /> },
    { id: 'stock', label: t('common:nav.stock'), icon: <Inventory2Icon /> },
    { id: 'users', label: t('common:nav.users'), icon: <ManageAccountsIcon /> },
    { id: 'maintenance-contracts', label: t('common:nav.maintenanceContracts'), icon: <EventRepeatIcon /> },
    { id: 'reports', label: t('common:nav.reports'), icon: <BarChartIcon /> },
  ]

  const mobileNavItems = navGroups.filter(g => ['schedule', 'customers', 'work-orders', 'quotes', 'quick-sale'].includes(g.id))
  const comingSoon = new Set(['maintenance-contracts', 'reports'])
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
              >
                {t('common:nav.testData')}
              </Button>
              <Button
                variant={location.pathname === '/endpoint-trigger' ? 'contained' : 'outlined'}
                size="small"
                color="secondary"
                onClick={() => navigate('/endpoint-trigger')}
              >
                {t('common:nav.endpointTrigger')}
              </Button>
              <Button variant="outlined" size="small" onClick={() => navigate('/login')}>
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

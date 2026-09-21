import { useMemo, useState, useEffect } from 'react'
import { Routes, Route, useNavigate, useLocation } from 'react-router-dom'
import { ThemeProvider } from '@mui/material/styles'
import CssBaseline from '@mui/material/CssBaseline'
import { AppBar, Toolbar, Typography, Drawer, Badge, Box, ButtonBase, IconButton, TextField, InputAdornment, BottomNavigation, BottomNavigationAction, useMediaQuery, Paper, Button, Table, TableBody, TableCell, TableContainer, TableHead, TableRow, Chip, Dialog, DialogTitle, DialogContent, Grid } from '@mui/material'
import SearchIcon from '@mui/icons-material/Search'
import MenuIcon from '@mui/icons-material/Menu'
import MenuBookIcon from '@mui/icons-material/MenuBook'
import Inventory2Icon from '@mui/icons-material/Inventory2'
import BarChartIcon from '@mui/icons-material/BarChart'
import CalendarMonthIcon from '@mui/icons-material/CalendarMonth'
import EventRepeatIcon from '@mui/icons-material/EventRepeat'
import NotificationsIcon from '@mui/icons-material/Notifications'
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
import { authApi, workOrdersApi, quotesApi, remindersApi, type AuthResult, type WorkOrder } from './api'
import { QuotesView } from './quotes/QuotesView'
import { QuoteDetailPage } from './quotes/QuoteDetailPage'
import { QuoteFormDialog } from './quotes/QuoteFormDialog'
import { CustomersView } from './customers/CustomersView'
import { CustomerDetailView } from './customers/CustomerDetailView'
import { WorkOrdersView } from './OperationsViews'
import { WorkOrderDetailPage } from './WorkOrderDetailPage'
import { InvoicesView } from './payments/InvoicesView'
import { InvoiceDetailView } from './payments/InvoiceDetailView'
import { PaymentsView } from './payments/PaymentsView'
import { StockView } from './StockView'
import { ProjectsView } from './projects/ProjectsView'
import { RemindersView } from './reminders/RemindersView'
import { CustomerFormDialog } from './customers/CustomerFormDialog'
import { CreateWorkOrderModal } from './CreateWorkOrderModal'
import { ProductIntakeView } from './ProductIntakeView'
import { QuickSaleView } from './QuickSaleView'
import { CatalogView } from './CatalogView'
import { SideNav } from './navigation/SideNav'
import { UserMenu } from './navigation/UserMenu'
import { findGroupId, findLabel, type NavGroup } from './navigation/navModel'
import { ComingSoonView } from './navigation/ComingSoonView'
import { SettingsView } from './settings/SettingsView'
import { UsersView } from './settings/UsersView'
import { ScheduleView } from './ScheduleView'
import { useI18n } from './i18n'

const drawerWidth = 240
const TERMINAL_STATUSES = new Set(['Invoiced', 'Cancelled', 'NoShow'])

function App() {
  const { lang, setLang, translate: t } = useI18n()
  const navigate = useNavigate()
  const location = useLocation()

  // Derive the current view id from the URL path
  const activeView = location.pathname === '/' ? 'dashboard' : location.pathname.split('/').filter(Boolean)[0]

  const navTo = (id: string) => navigate(id === 'dashboard' ? '/' : '/' + id)

  const prefersDarkMode = useMediaQuery('(prefers-color-scheme: dark)')
  const [mode, setMode] = useState<'light' | 'dark'>(prefersDarkMode ? 'dark' : 'light')
  const theme = useMemo(() => getTheme(mode), [mode])

  const [dashboardOrders, setDashboardOrders] = useState<WorkOrder[]>([])
  const [pendingQuotes, setPendingQuotes] = useState(0)
  const [unassignedCount, setUnassignedCount] = useState(0)
  const [session, setSession] = useState<AuthResult | null>(() => {
    const stored = localStorage.getItem('voltflow.session')
    return stored ? (JSON.parse(stored) as AuthResult) : null
  })
  const [mobileNavOpen, setMobileNavOpen] = useState(false)
  const [pendingReminders, setPendingReminders] = useState(0)

  useEffect(() => {
    if (session && location.pathname === '/') {
      workOrdersApi.list().then((page) => setDashboardOrders(page.items)).catch(() => {})
      quotesApi.page({ state: 'Issued', limit: 1 }).then((page) => setPendingQuotes(page.total)).catch(() => {})
      workOrdersApi.list({ limit: 50 }).then((page) => {
        setUnassignedCount(page.items.filter(o => !o.assignedUserId && !TERMINAL_STATUSES.has(o.status)).length)
      }).catch(() => {})
    }
  }, [session, location.pathname])

  useEffect(() => {
    if (session) remindersApi.list({ state: 'Pending', limit: 1 }).then((page) => setPendingReminders(page.total)).catch(() => {})
  }, [session, location.pathname])

  const activeOrderCount = dashboardOrders.filter(o => !TERMINAL_STATUSES.has(o.status)).length

  const navGroups: NavGroup[] = [
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

  const [search, setSearch] = useState('')
  const [showQuickCreate, setShowQuickCreate] = useState(false)
  const [quickAction, setQuickAction] = useState<'customer' | 'quote' | 'workorder' | null>(null)

  const filteredOrders = useMemo(() => {
    const query = search.toLowerCase().trim()
    if (!query) return dashboardOrders
    return dashboardOrders.filter((order) =>
      [order.number, order.title, order.customerId, order.assignedUserId || 'Unassigned'].some((value) =>
        value.toLowerCase().includes(query)
      )
    )
  }, [search, dashboardOrders])

  const isMobile = useMediaQuery(theme.breakpoints.down('sm'))

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
      <Box sx={{ display: 'flex', height: '100vh', overflow: 'hidden' }}>

        <AppBar position="fixed" sx={{ zIndex: (theme) => theme.zIndex.drawer + 1, bgcolor: 'background.paper', color: 'text.primary', borderBottom: 1, borderColor: 'divider', boxShadow: 'none' }}>
          <Toolbar>
            {isMobile && (
              <IconButton edge="start" onClick={() => setMobileNavOpen(true)} aria-label={t('common:nav.openMenu')} data-testid="nav-open-menu">
                <MenuIcon />
              </IconButton>
            )}
            <ButtonBase
              onClick={() => navTo('dashboard')}
              data-testid="nav-home"
              sx={{ width: { xs: 'auto', sm: drawerWidth - 24 }, justifyContent: 'flex-start' }}
            >
              <Typography variant="h6" noWrap component="span" sx={{ fontWeight: 700, color: 'primary.main' }}>
                {t('common:brand.name')}
              </Typography>
            </ButtonBase>

            <Box sx={{ flexGrow: 1 }} />

            <IconButton
              onClick={() => navTo('reminders')}
              aria-label={t('common:nav.reminders')}
              color={activeView === 'reminders' ? 'primary' : 'default'}
              data-testid="nav-reminders"
            >
              <Badge badgeContent={pendingReminders} color="primary" max={99}>
                <NotificationsIcon />
              </Badge>
            </IconButton>

            <UserMenu
              initials={session.name.split(' ').map((p) => p[0]).join('').slice(0, 2)}
              lang={lang}
              mode={mode}
              onNavigate={navTo}
              onToggleLang={() => setLang(lang === 'en' ? 'tr' : 'en')}
              onToggleMode={() => setMode(mode === 'dark' ? 'light' : 'dark')}
              onLogout={handleLogout}
            />
          </Toolbar>
        </AppBar>

        {!isMobile && (
          <Drawer
            variant="permanent"
            sx={{
              width: drawerWidth,
              flexShrink: 0,
              [`& .MuiDrawer-paper`]: { width: drawerWidth, boxSizing: 'border-box' },
            }}
          >
            <Toolbar />
            <Box sx={{ overflow: 'auto' }}>
              <SideNav
                groups={navGroups}
                activeView={activeView}
                onNavigate={navTo}
                ariaLabel={t('common:nav.mainNavigation')}
                create={{ label: t('common:actions.create'), onClick: () => setShowQuickCreate(true) }}
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
                create={{ label: t('common:actions.create'), onClick: () => { setMobileNavOpen(false); setShowQuickCreate(true) } }}
              />
            </Box>
          </Drawer>
        )}

        <Box component="main" sx={{ flexGrow: 1, bgcolor: 'background.default', p: 3, pt: 10, pb: { xs: 9, sm: 3 }, overflow: 'auto' }}>
          <Routes>
            <Route path="/" element={
              <Box sx={{ display: 'flex', flexDirection: { xs: 'column', md: 'row' }, gap: 3 }}>
                <Box sx={{ flex: 1, minWidth: 0 }}>
                  <Box sx={{ display: 'flex', justifyContent: 'space-between', mb: 3 }}>
                    <Typography variant="h4">{t('common:nav.overview')}</Typography>
                  </Box>
                  <Grid container spacing={2}>
                    <Grid size={{ xs: 12, sm: 6 }}>
                      <Paper sx={{ p: 3, borderTop: '4px solid', borderColor: 'primary.main' }}>
                        <Typography color="text.secondary" gutterBottom>{t('common:dashboard.activeOrders')}</Typography>
                        <Typography variant="h3">{activeOrderCount}</Typography>
                      </Paper>
                    </Grid>
                    <Grid size={{ xs: 12, sm: 6 }}>
                      <Paper sx={{ p: 3, borderTop: '4px solid #6cb38a' }}>
                        <Typography color="text.secondary" gutterBottom>{t('common:dashboard.pendingQuotes')}</Typography>
                        <Typography variant="h3">{pendingQuotes}</Typography>
                      </Paper>
                    </Grid>
                  </Grid>

                  {(unassignedCount > 0 || pendingQuotes > 0) && (
                    <Paper sx={{ mt: 3, p: 2, border: 1, borderColor: 'warning.light', bgcolor: 'warning.50' }}>
                      <Typography variant="subtitle2" gutterBottom sx={{ fontWeight: 600 }}>
                        {t('common:dashboard.attentionNeeded')}
                      </Typography>
                      {unassignedCount > 0 && (
                        <Box sx={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', py: 0.5 }}>
                          <Typography variant="body2">
                            <strong>{unassignedCount}</strong> {t('common:dashboard.unassignedOrders')}
                          </Typography>
                          <Button size="small" onClick={() => navTo('work-orders')}>{t('common:dashboard.assign')}</Button>
                        </Box>
                      )}
                      {pendingQuotes > 0 && (
                        <Box sx={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', py: 0.5 }}>
                          <Typography variant="body2">
                            <strong>{pendingQuotes}</strong> {t('common:dashboard.quotesPending')}
                          </Typography>
                          <Button size="small" onClick={() => navTo('quotes')}>{t('common:dashboard.review')}</Button>
                        </Box>
                      )}
                    </Paper>
                  )}

                  <Box sx={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', gap: 2, flexWrap: 'wrap', mt: 4, mb: 2 }}>
                    <Typography variant="h5">{t('common:dashboard.recentOrders')}</Typography>
                    <TextField
                      size="small"
                      placeholder={t('common:dashboard.filterOrders')}
                      value={search}
                      onChange={(e) => setSearch(e.target.value)}
                      sx={{ minWidth: 260 }}
                      slotProps={{ input: { startAdornment: <InputAdornment position="start"><SearchIcon /></InputAdornment> } }}
                      data-testid="dashboard-orders-filter"
                    />
                  </Box>
                  <TableContainer component={Paper}>
                    <Table>
                      <TableHead>
                        <TableRow>
                          <TableCell><strong>{t('common:fields.number')}</strong></TableCell>
                          <TableCell><strong>{t('common:fields.title')}</strong></TableCell>
                          <TableCell><strong>{t('common:fields.status')}</strong></TableCell>
                        </TableRow>
                      </TableHead>
                      <TableBody>
                        {filteredOrders.map(o => (
                          <TableRow key={o.id} hover sx={{ cursor: 'pointer' }} onClick={() => navigate('/work-orders/' + o.id)}>
                            <TableCell><strong>{o.number}</strong></TableCell>
                            <TableCell>{o.title}</TableCell>
                            <TableCell>
                              <Chip
                                label={o.status}
                                size="small"
                                color={o.status === 'Completed' ? 'success' : o.status === 'InProgress' ? 'warning' : 'default'}
                              />
                            </TableCell>
                          </TableRow>
                        ))}
                      </TableBody>
                    </Table>
                  </TableContainer>
                </Box>
                <Box sx={{ width: { xs: '100%', md: 300 }, flexShrink: 0 }}>
                  <Typography variant="h6" gutterBottom>{t('common:dashboard.quickActions')}</Typography>
                  <Button variant="contained" fullWidth sx={{ mb: 2 }} onClick={() => setShowQuickCreate(true)}>
                    + {t('common:actions.create')}
                  </Button>
                </Box>
              </Box>
            } />

            <Route path="/schedule" element={<ScheduleView />} />
            <Route path="/customers" element={<CustomersView />} />
            <Route path="/customers/:id" element={<CustomerDetailView />} />
            <Route path="/quotes" element={<QuotesView />} />
            <Route path="/quotes/:id" element={<QuoteDetailPage />} />
            <Route path="/work-orders" element={<WorkOrdersView />} />
            <Route path="/work-orders/:id" element={<WorkOrderDetailPage />} />
            <Route path="/invoices" element={<InvoicesView />} />
            <Route path="/invoices/:id" element={<InvoiceDetailView />} />
            <Route path="/payments" element={<PaymentsView />} />
            <Route path="/quick-sale" element={<QuickSaleView section="sell" onNavigate={navTo} />} />
            <Route path="/sale-history" element={<QuickSaleView section="history" onNavigate={navTo} />} />
            <Route path="/cash-shift" element={<QuickSaleView section="shift" onNavigate={navTo} />} />
            <Route path="/catalog" element={<CatalogView />} />
            <Route path="/stock" element={<StockView />} />
            <Route path="/projects" element={<ProjectsView />} />
            <Route path="/reminders" element={<RemindersView />} />
            <Route path="/product-intake" element={<ProductIntakeView />} />
            <Route path="/users" element={<UsersView />} />
            <Route path="/settings" element={<SettingsView />} />
            {[...comingSoon].map(id => (
              <Route key={id} path={'/' + id} element={<ComingSoonView title={findLabel(navGroups, id) ?? ''} />} />
            ))}
          </Routes>

          <Dialog open={showQuickCreate} onClose={() => setShowQuickCreate(false)} maxWidth="xs" fullWidth>
            <DialogTitle>{t('common:dashboard.quickActions')}</DialogTitle>
            <DialogContent>
              <Box sx={{ display: 'flex', flexDirection: 'column', gap: 2, mt: 1 }}>
                <Button variant="outlined" onClick={() => { setQuickAction('workorder'); setShowQuickCreate(false) }}>{t('common:actions.createWorkOrder')}</Button>
                <Button variant="outlined" onClick={() => { setQuickAction('customer'); setShowQuickCreate(false) }}>{t('common:actions.createCustomer')}</Button>
                <Button variant="outlined" onClick={() => { setQuickAction('quote'); setShowQuickCreate(false) }}>{t('common:actions.createQuote')}</Button>
              </Box>
            </DialogContent>
          </Dialog>

          {quickAction === 'customer' && <CustomerFormDialog customer={null} onClose={() => setQuickAction(null)} onSaved={() => setQuickAction(null)} />}
          {quickAction === 'quote' && (
            <QuoteFormDialog
              quote={null}
              onClose={() => setQuickAction(null)}
              onSaved={() => { setQuickAction(null); navTo('quotes') }}
            />
          )}
          {quickAction === 'workorder' && <CreateWorkOrderModal onClose={() => setQuickAction(null)} onSuccess={() => setQuickAction(null)} />}
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
    </ThemeProvider>
  )
}

export default App

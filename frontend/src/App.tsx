import { useMemo, useState, useEffect } from 'react'
import { ThemeProvider } from '@mui/material/styles'
import CssBaseline from '@mui/material/CssBaseline'
import { AppBar, Toolbar, Typography, Drawer, Box, ButtonBase, IconButton, TextField, InputAdornment, BottomNavigation, BottomNavigationAction, useMediaQuery, Paper, Button, Table, TableBody, TableCell, TableContainer, TableHead, TableRow, Chip, Dialog, DialogTitle, DialogContent, Grid } from '@mui/material'
import SearchIcon from '@mui/icons-material/Search'
import MenuIcon from '@mui/icons-material/Menu'
import Inventory2Icon from '@mui/icons-material/Inventory2'
import PeopleIcon from '@mui/icons-material/People'
import WorkIcon from '@mui/icons-material/Work'
import PaymentIcon from '@mui/icons-material/Payment'
import PointOfSaleIcon from '@mui/icons-material/PointOfSale'

import { getTheme } from './theme'
import './App.css'
import { AuthView } from './AuthView'
import { authApi, workOrdersApi, quotesApi, type AuthResult, type WorkOrder } from './api'
import { QuotesView } from './quotes/QuotesView'
import { QuoteFormDialog } from './quotes/QuoteFormDialog'
import { CustomersView } from './customers/CustomersView'
import { WorkOrdersView, AuditLogsView } from './OperationsViews'
import { InvoicesView } from './payments/InvoicesView'
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
import { useI18n } from './i18n'

const drawerWidth = 240;

function App() {
  const { lang, setLang, translate: t } = useI18n()
  
  // Theme State
  const prefersDarkMode = useMediaQuery('(prefers-color-scheme: dark)')
  const [mode, setMode] = useState<'light' | 'dark'>(prefersDarkMode ? 'dark' : 'light')
  const theme = useMemo(() => getTheme(mode), [mode])

  const [dashboardOrders, setDashboardOrders] = useState<WorkOrder[]>([])
  const [pendingQuotes, setPendingQuotes] = useState(0)
  const [session, setSession] = useState<AuthResult | null>(() => {
    const stored = localStorage.getItem('voltflow.session')
    return stored ? (JSON.parse(stored) as AuthResult) : null
  })
  const [activeView, setActiveView] = useState('dashboard')
  const [mobileNavOpen, setMobileNavOpen] = useState(false)

  useEffect(() => {
    if (session && activeView === 'dashboard') {
      workOrdersApi.list().then((page) => setDashboardOrders(page.items)).catch(() => {})
      // the quotes waiting for the customer's answer
      quotesApi.page({ state: 'Issued', limit: 1 }).then((page) => setPendingQuotes(page.total)).catch(() => {})
    }
  }, [session, activeView])

  // A group with `items` is an accordion in the left menu; one without is a destination itself.
  // The dashboard is the logo's page and reports sit in the avatar menu, so neither is a group here.
  const navGroups: NavGroup[] = [
    { id: 'customers', label: t('common:nav.customers'), icon: <PeopleIcon /> },
    {
      id: 'sales',
      label: t('common:nav.sales'),
      icon: <PointOfSaleIcon />,
      items: [
        { id: 'quotes', label: t('common:nav.quotes') },
        { id: 'quick-sale', label: t('common:nav.quickSale') },
        { id: 'sale-history', label: t('common:nav.saleHistory') },
        { id: 'cash-shift', label: t('common:nav.cashShift') },
      ],
    },
    {
      id: 'jobs',
      label: t('common:nav.jobs'),
      icon: <WorkIcon />,
      badge: dashboardOrders.length > 0 ? dashboardOrders.length : undefined,
      items: [
        { id: 'work-orders', label: t('common:nav.workOrders') },
        { id: 'product-intake', label: t('common:nav.productIntake') },
        { id: 'reminders', label: t('common:nav.reminders') },
        { id: 'schedule', label: t('common:nav.schedule') },
        { id: 'route', label: t('common:nav.route') },
        { id: 'projects', label: t('common:nav.projects') },
        { id: 'maintenance-contracts', label: t('common:nav.maintenanceContracts') },
      ],
    },
    {
      id: 'finance',
      label: t('common:nav.finance'),
      icon: <PaymentIcon />,
      items: [
        { id: 'invoices', label: t('common:nav.invoices') },
        { id: 'payments', label: t('common:nav.payments') },
      ],
    },
    {
      id: 'catalog-stock',
      label: t('common:nav.catalogStock'),
      icon: <Inventory2Icon />,
      items: [
        { id: 'catalog', label: t('common:nav.catalog') },
        { id: 'stock', label: t('common:nav.stock') },
      ],
    },
  ]

  // the modules of the menu that have no screen yet
  const comingSoon = new Set(['schedule', 'route', 'maintenance-contracts'])

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
      try {
        await authApi.revokeSession(session.token)
      } catch (err) {
        console.error('Logout failed', err)
      }
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
              onClick={() => setActiveView('dashboard')}
              data-testid="nav-home"
              sx={{ width: { xs: 'auto', sm: drawerWidth - 24 }, justifyContent: 'flex-start' }}
            >
              <Typography variant="h6" noWrap component="span" sx={{ fontWeight: 700, color: 'primary.main' }}>
                {t('common:brand.name')}
              </Typography>
            </ButtonBase>
            
            <Box sx={{ flexGrow: 1 }} />
            
            <UserMenu
              initials={session.name.split(' ').map((p) => p[0]).join('').slice(0, 2)}
              lang={lang}
              mode={mode}
              onNavigate={setActiveView}
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
              <SideNav groups={navGroups} activeView={activeView} onNavigate={setActiveView} ariaLabel={t('common:nav.mainNavigation')} />
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
                onNavigate={(viewId) => { setActiveView(viewId); setMobileNavOpen(false) }}
                ariaLabel={t('common:nav.mainNavigation')}
              />
            </Box>
          </Drawer>
        )}

        <Box component="main" sx={{ flexGrow: 1, bgcolor: 'background.default', p: 3, pt: 10, pb: { xs: 9, sm: 3 }, overflow: 'auto' }}>
          {activeView === 'dashboard' && (
            <Box sx={{ display: 'flex', flexDirection: { xs: 'column', md: 'row' }, gap: 3 }}>
              <Box sx={{ flex: 1, minWidth: 0 }}>
                <Box sx={{ display: 'flex', justifyContent: 'space-between', mb: 3 }}>
                  <Typography variant="h4">{t('common:nav.overview')}</Typography>
                </Box>
                {dashboardOrders.length === 0 && pendingQuotes === 0 ? (
                  <Paper sx={{ p: 4, textAlign: 'center' }}>
                    <Typography variant="h6" color="text.secondary">{t('common:dashboard.noActivity')}</Typography>
                    <Button variant="contained" sx={{ mt: 2 }} onClick={() => setShowQuickCreate(true)}>
                      {t('common:dashboard.quickActions')}
                    </Button>
                  </Paper>
                ) : (
                  <Grid container spacing={2}>
                    <Grid size={{ xs: 12, sm: 6 }}>
                      <Paper sx={{ p: 3, borderTop: '4px solid', borderColor: 'primary.main' }}>
                        <Typography color="text.secondary" gutterBottom>{t('common:dashboard.activeOrders')}</Typography>
                        <Typography variant="h3">{dashboardOrders.length}</Typography>
                      </Paper>
                    </Grid>
                    <Grid size={{ xs: 12, sm: 6 }}>
                      <Paper sx={{ p: 3, borderTop: '4px solid #6cb38a' }}>
                        <Typography color="text.secondary" gutterBottom>{t('common:dashboard.pendingQuotes')}</Typography>
                        <Typography variant="h3">{pendingQuotes}</Typography>
                      </Paper>
                    </Grid>
                  </Grid>
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
                        <TableRow key={o.id}>
                          <TableCell><strong>{o.number}</strong></TableCell>
                          <TableCell>{o.title}</TableCell>
                          <TableCell>
                            <Chip 
                              label={o.status} 
                              size="small" 
                              color={o.status === 'Completed' ? 'success' : o.status === 'In Progress' ? 'warning' : 'default'} 
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
          )}

          {activeView === 'customers' && <CustomersView />}
          {activeView === 'quotes' && <QuotesView />}
          {activeView === 'work-orders' && <WorkOrdersView />}
          {activeView === 'stock' && <StockView />}
          {activeView === 'catalog' && <CatalogView />}
          {activeView === 'invoices' && <InvoicesView />}
          {activeView === 'payments' && <PaymentsView />}
          {activeView === 'projects' && <ProjectsView />}
          {activeView === 'reminders' && <RemindersView />}
          {activeView === 'product-intake' && <ProductIntakeView />}
          {activeView === 'quick-sale' && <QuickSaleView section="sell" onNavigate={setActiveView} />}
          {activeView === 'sale-history' && <QuickSaleView section="history" onNavigate={setActiveView} />}
          {activeView === 'cash-shift' && <QuickSaleView section="shift" onNavigate={setActiveView} />}
          {comingSoon.has(activeView) && <ComingSoonView title={findLabel(navGroups, activeView) ?? ''} />}
          {activeView === 'reports' && <ComingSoonView title={t('common:nav.reports')} />}
          {activeView === 'audit-log' && <AuditLogsView />}

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
              onSaved={() => {
                setQuickAction(null)
                setActiveView('quotes')
              }}
            />
          )}
          {quickAction === 'workorder' && <CreateWorkOrderModal onClose={() => setQuickAction(null)} onSuccess={() => setQuickAction(null)} />}
        </Box>

        {isMobile && (
          <BottomNavigation
            value={activeGroupId ?? false}
            onChange={(_, newValue: string) => {
              const group = navGroups.find((g) => g.id === newValue)
              setActiveView(group?.items ? group.items[0].id : newValue)
            }}
            showLabels
            sx={{ position: 'fixed', bottom: 0, left: 0, right: 0, zIndex: 1000, borderTop: 1, borderColor: 'divider' }}
          >
            {navGroups.map((item) => (
              <BottomNavigationAction key={item.id} label={item.label} value={item.id} icon={item.icon} />
            ))}
          </BottomNavigation>
        )}
      </Box>
    </ThemeProvider>
  )
}

export default App

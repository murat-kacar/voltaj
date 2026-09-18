import { useMemo, useState, useEffect } from 'react'
import { ThemeProvider } from '@mui/material/styles'
import CssBaseline from '@mui/material/CssBaseline'
import { AppBar, Toolbar, Typography, Drawer, List, ListItem, ListItemButton, ListItemIcon, ListItemText, Box, IconButton, Avatar, TextField, InputAdornment, BottomNavigation, BottomNavigationAction, useMediaQuery, Badge, Paper, Button, Table, TableBody, TableCell, TableContainer, TableHead, TableRow, Chip, Dialog, DialogTitle, DialogContent, Grid } from '@mui/material'
import SearchIcon from '@mui/icons-material/Search'
import Brightness4Icon from '@mui/icons-material/Brightness4'
import Brightness7Icon from '@mui/icons-material/Brightness7'
import HomeIcon from '@mui/icons-material/Home'
import PeopleIcon from '@mui/icons-material/People'
import RequestQuoteIcon from '@mui/icons-material/RequestQuote'
import WorkIcon from '@mui/icons-material/Work'
import InventoryIcon from '@mui/icons-material/Inventory'
import PaymentIcon from '@mui/icons-material/Payment'
import Inventory2Icon from '@mui/icons-material/Inventory2'
import SettingsIcon from '@mui/icons-material/Settings'
import ListAltIcon from '@mui/icons-material/ListAlt'
import LanguageIcon from '@mui/icons-material/Language'

import { getTheme } from './theme'
import './App.css'
import { AuthView } from './AuthView'
import { authApi, workOrdersApi, quotesApi, type AuthResult, type WorkOrder, type Quote } from './api'
import { CustomersView, QuotesView } from './ModuleViews'
import { InventoryView, PaymentsView, WorkOrdersView, AuditLogsView } from './OperationsViews'
import { CreateCustomerModal } from './CreateCustomerModal'
import { CreateQuoteModal } from './CreateQuoteModal'
import { CreateWorkOrderModal } from './CreateWorkOrderModal'
import { ProductIntakeView } from './ProductIntakeView'
import { useI18n } from './i18n'

const drawerWidth = 240;

function App() {
  const { lang, setLang, translate: t } = useI18n()
  
  // Theme State
  const prefersDarkMode = useMediaQuery('(prefers-color-scheme: dark)')
  const [mode, setMode] = useState<'light' | 'dark'>(prefersDarkMode ? 'dark' : 'light')
  const theme = useMemo(() => getTheme(mode), [mode])

  const [dashboardOrders, setDashboardOrders] = useState<WorkOrder[]>([])
  const [dashboardQuotes, setDashboardQuotes] = useState<Quote[]>([])
  const [session, setSession] = useState<AuthResult | null>(() => {
    const stored = localStorage.getItem('voltflow.session')
    return stored ? (JSON.parse(stored) as AuthResult) : null
  })
  const [activeView, setActiveView] = useState('Overview')
  
  useEffect(() => {
    if (session && activeView === 'Overview') {
      workOrdersApi.list().then(setDashboardOrders).catch(() => {})
      quotesApi.list().then(setDashboardQuotes).catch(() => {})
    }
  }, [session, activeView])

  const navigation = [
    { id: 'Overview', label: t('common:nav.overview'), icon: <HomeIcon /> },
    { id: 'Customers', label: t('common:nav.customers'), icon: <PeopleIcon /> },
    { id: 'Quotes', label: t('common:nav.quotes'), icon: <RequestQuoteIcon /> },
    { id: 'Work orders', label: t('common:nav.workOrders'), icon: <WorkIcon />, count: dashboardOrders.length > 0 ? dashboardOrders.length : undefined },
    { id: 'Product Intake', label: t('common:nav.productIntake') || 'Ürün Kabul', icon: <Inventory2Icon /> },
    { id: 'Inventory', label: t('common:nav.inventory'), icon: <InventoryIcon /> },
    { id: 'Payments', label: t('common:nav.payments'), icon: <PaymentIcon /> },
  ]

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
            <Typography variant="h6" noWrap component="div" sx={{ width: drawerWidth - 24, fontWeight: 700, color: 'primary.main' }}>
              {t('common:brand.name')}
            </Typography>
            
            <TextField
              size="small"
              placeholder={t('common:actions.searchPlaceholder')}
              value={search}
              onChange={(e) => setSearch(e.target.value)}
              sx={{ flexGrow: 1, maxWidth: 400, mx: 2 }}
              slotProps={{ input: { startAdornment: <InputAdornment position="start"><SearchIcon /></InputAdornment> } }}
            />
            
            <Box sx={{ flexGrow: 1 }} />
            
            <IconButton onClick={() => setLang(lang === 'en' ? 'tr' : 'en')} title={lang === 'en' ? 'Türkçe' : 'English'}>
              <LanguageIcon />
            </IconButton>
            
            <IconButton onClick={() => setMode(mode === 'dark' ? 'light' : 'dark')} color="inherit">
              {mode === 'dark' ? <Brightness7Icon /> : <Brightness4Icon />}
            </IconButton>
            
            <IconButton onClick={() => setActiveView('Audit log')} title={t('common:nav.auditLog')}>
              <ListAltIcon />
            </IconButton>
            
            <IconButton onClick={() => setActiveView('Admin')} title={t('common:nav.admin')}>
              <SettingsIcon />
            </IconButton>

            <IconButton onClick={handleLogout} sx={{ ml: 1 }}>
              <Avatar sx={{ width: 32, height: 32, bgcolor: 'primary.main', fontSize: 14 }}>
                {session.name.split(' ').map((p) => p[0]).join('').slice(0, 2)}
              </Avatar>
            </IconButton>
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
              <List>
                {navigation.map((item) => (
                  <ListItem key={item.id} disablePadding>
                    <ListItemButton selected={activeView === item.id} onClick={() => setActiveView(item.id)}>
                      <ListItemIcon>
                        {item.count ? <Badge badgeContent={item.count} color="primary">{item.icon}</Badge> : item.icon}
                      </ListItemIcon>
                      <ListItemText primary={item.label} />
                    </ListItemButton>
                  </ListItem>
                ))}
              </List>
            </Box>
          </Drawer>
        )}

        <Box component="main" sx={{ flexGrow: 1, bgcolor: 'background.default', p: 3, pt: 10, overflow: 'auto' }}>
          {activeView === 'Overview' && (
            <Box sx={{ display: 'flex', flexDirection: { xs: 'column', md: 'row' }, gap: 3 }}>
              <Box sx={{ flex: 1, minWidth: 0 }}>
                <Box sx={{ display: 'flex', justifyContent: 'space-between', mb: 3 }}>
                  <Typography variant="h4">{t('common:nav.overview')}</Typography>
                </Box>
                {dashboardOrders.length === 0 && dashboardQuotes.length === 0 ? (
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
                        <Typography variant="h3">{dashboardQuotes.length}</Typography>
                      </Paper>
                    </Grid>
                  </Grid>
                )}
                
                <Typography variant="h5" sx={{ mt: 4, mb: 2 }}>{t('common:dashboard.recentOrders')}</Typography>
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

          {activeView === 'Customers' && <CustomersView />}
          {activeView === 'Quotes' && <QuotesView />}
          {activeView === 'Work orders' && <WorkOrdersView />}
          {activeView === 'Inventory' && <InventoryView />}
          {activeView === 'Payments' && <PaymentsView />}
          {activeView === 'Product Intake' && <ProductIntakeView />}
          {activeView === 'Audit log' && <AuditLogsView />}

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

          {quickAction === 'customer' && <CreateCustomerModal onClose={() => setQuickAction(null)} onSuccess={() => setQuickAction(null)} />}
          {quickAction === 'quote' && <CreateQuoteModal onClose={() => setQuickAction(null)} onSuccess={() => setQuickAction(null)} />}
          {quickAction === 'workorder' && <CreateWorkOrderModal onClose={() => setQuickAction(null)} onSuccess={() => setQuickAction(null)} />}
        </Box>

        {isMobile && (
          <BottomNavigation
            value={activeView}
            onChange={(_, newValue) => setActiveView(newValue)}
            showLabels
            sx={{ position: 'fixed', bottom: 0, left: 0, right: 0, zIndex: 1000, borderTop: 1, borderColor: 'divider' }}
          >
            {navigation.slice(0, 4).map((item) => (
              <BottomNavigationAction key={item.id} label={item.label} value={item.id} icon={item.icon} />
            ))}
          </BottomNavigation>
        )}
      </Box>
    </ThemeProvider>
  )
}

export default App

import { useState, useMemo, useEffect } from 'react'
import { useNavigate } from 'react-router-dom'
import { Box, Typography, Grid, Paper, Button, TextField, InputAdornment, TableContainer, Table, TableHead, TableRow, TableCell, TableBody, Chip, Dialog, DialogTitle, DialogContent } from '@mui/material'
import SearchIcon from '@mui/icons-material/Search'
import AutoFixHighIcon from '@mui/icons-material/AutoFixHigh'

import { useI18n } from '../i18n'
import { workOrdersApi, quotesApi, type WorkOrder } from '../api'
import { CustomerFormDialog } from '../customers/CustomerFormDialog'
import { QuoteFormDialog } from '../quotes/QuoteFormDialog'
import { CreateWorkOrderModal } from '../CreateWorkOrderModal'

const TERMINAL_STATUSES = new Set(['Invoiced', 'Cancelled', 'NoShow'])

export function DashboardView() {
  const { translate: t } = useI18n()
  const navigate = useNavigate()

  const [dashboardOrders, setDashboardOrders] = useState<WorkOrder[]>([])
  const [pendingQuotes, setPendingQuotes] = useState(0)
  const [unassignedCount, setUnassignedCount] = useState(0)
  const [search, setSearch] = useState('')
  const [showQuickCreate, setShowQuickCreate] = useState(false)
  const [quickAction, setQuickAction] = useState<'customer' | 'quote' | 'workorder' | null>(null)

  useEffect(() => {
    workOrdersApi.list().then((page) => setDashboardOrders(page.items)).catch(() => {})
    quotesApi.page({ state: 'Issued', limit: 1 }).then((page) => setPendingQuotes(page.total)).catch(() => {})
    workOrdersApi.list({ limit: 50 }).then((page) => {
      setUnassignedCount(page.items.filter(o => !o.assignedUserId && !TERMINAL_STATUSES.has(o.status)).length)
    }).catch(() => {})
  }, [])

  const activeOrderCount = dashboardOrders.filter(o => !TERMINAL_STATUSES.has(o.status)).length

  const filteredOrders = useMemo(() => {
    const query = search.toLowerCase().trim()
    if (!query) return dashboardOrders
    return dashboardOrders.filter((order) =>
      [order.number, order.title, order.customerId, order.assignedUserId || 'Unassigned'].some((value) =>
        value.toLowerCase().includes(query)
      )
    )
  }, [search, dashboardOrders])

  const navTo = (path: string) => navigate(`/${path}`)

  return (
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
            <Paper sx={{ p: 3, borderTop: '4px solid', borderColor: 'secondary.main' }}>
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
        <Button
          variant="outlined"
          color="primary"
          fullWidth
          sx={{ mb: 2 }}
          startIcon={<AutoFixHighIcon />}
          onClick={() => navTo('test-data')}
          data-testid="dashboard-btn-test-data"
        >
          {t('common:generator.title')}
        </Button>
      </Box>

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
  )
}

import { useEffect, useMemo, useState } from 'react'
import { Box, Typography, Button, TextField, InputAdornment, Paper, Table, TableBody, TableCell, TableContainer, TableHead, TableRow, Chip, IconButton, CircularProgress, Alert, Tabs, Tab } from '@mui/material'
import SearchIcon from '@mui/icons-material/Search'
import FilterListIcon from '@mui/icons-material/FilterList'
import AddIcon from '@mui/icons-material/Add'
import VisibilityIcon from '@mui/icons-material/Visibility'
import { workOrdersApi, type WorkOrder as ApiWorkOrder } from './api'
import { CreateWorkOrderModal } from './CreateWorkOrderModal'
import { WorkOrderDetailDrawer } from './WorkOrderDetailDrawer'
import { useI18n, formatCurrency } from './i18n'

export function WorkOrdersView() {
  const { translate: t } = useI18n()
  const sessionData = localStorage.getItem('voltflow.session')
  const currentUserId = sessionData ? JSON.parse(sessionData).userId : null

  const [tab, setTab] = useState('All')
  const [query, setQuery] = useState('')
  const [rawOrders, setRawOrders] = useState<ApiWorkOrder[] | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')
  const [showModal, setShowModal] = useState(false)
  const [selectedOrder, setSelectedOrder] = useState<ApiWorkOrder | null>(null)
  const [reloadKey, setReloadKey] = useState(0)

  useEffect(() => {
    let ignore = false
    workOrdersApi
      .list()
      .then((items) => {
        if (!ignore) {
          setRawOrders(items)
          setError('')
          setLoading(false)
        }
      })
      .catch((reason: unknown) => {
        if (!ignore) {
          setError(reason instanceof Error ? reason.message : 'Error')
          setLoading(false)
        }
      })
    return () => {
      ignore = true
    }
  }, [reloadKey])

  const sourceOrders = useMemo(() => rawOrders ?? [], [rawOrders])
  const filtered = useMemo(
    () =>
      sourceOrders
        .filter((order) =>
          tab === 'Mine'
            ? order.assignedUserId === currentUserId
            : tab === 'Unassigned'
            ? !order.assignedUserId
            : true
        )
        .filter((order) =>
          `${order.number} ${order.title} ${order.customerId}`.toLowerCase().includes(query.toLowerCase())
        ),
    [query, tab, sourceOrders]
  )

  return (
    <Box>
      <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 3 }}>
        <Box>
          <Typography variant="overline" color="text.secondary">Work Orders</Typography>
          <Typography variant="h4">{t('common:views.workOrdersTitle')}</Typography>
        </Box>
        <Button variant="contained" startIcon={<AddIcon />} onClick={() => setShowModal(true)}>
          {t('common:modals.newWorkOrder')}
        </Button>
      </Box>

      {loading && <Box sx={{ display: 'flex', justifyContent: 'center', p: 4 }}><CircularProgress /></Box>}
      {error && <Alert severity="error" sx={{ mb: 3 }} action={<Button color="inherit" size="small" onClick={() => setReloadKey(k => k+1)}>Retry</Button>}>{error}</Alert>}

      {!loading && !error && (
        <Paper sx={{ width: '100%', mb: 2 }}>
          <Tabs value={tab} onChange={(e, v) => setTab(v)} sx={{ borderBottom: 1, borderColor: 'divider', px: 2, pt: 1 }}>
            <Tab label="All" value="All" />
            <Tab label="Mine" value="Mine" />
            <Tab label="Unassigned" value="Unassigned" />
          </Tabs>
          
          <Box sx={{ p: 2, display: 'flex', gap: 2, alignItems: 'center' }}>
            <TextField
              size="small"
              placeholder="Search work orders..."
              value={query}
              onChange={(e) => setQuery(e.target.value)}
              InputProps={{
                startAdornment: <InputAdornment position="start"><SearchIcon /></InputAdornment>,
              }}
              sx={{ flexGrow: 1, maxWidth: 400 }}
            />
            <Button variant="outlined" startIcon={<FilterListIcon />}>Filter</Button>
          </Box>
          <TableContainer>
            <Table>
              <TableHead>
                <TableRow>
                  <TableCell>Number</TableCell>
                  <TableCell>Title</TableCell>
                  <TableCell>{t('common:fields.customer')}</TableCell>
                  <TableCell>Assigned</TableCell>
                  <TableCell>Status</TableCell>
                  <TableCell align="right">{t('common:fields.actions')}</TableCell>
                </TableRow>
              </TableHead>
              <TableBody>
                {filtered.length === 0 ? (
                  <TableRow><TableCell colSpan={6} align="center">No data found</TableCell></TableRow>
                ) : (
                  filtered.map((order) => (
                    <TableRow key={order.id} hover>
                      <TableCell><strong>{order.number}</strong></TableCell>
                      <TableCell>{order.title}</TableCell>
                      <TableCell>{order.customerId}</TableCell>
                      <TableCell>{order.assignedUserId || <Typography variant="caption" color="text.secondary">Unassigned</Typography>}</TableCell>
                      <TableCell>
                        <Chip label={order.status} size="small" color={order.status === 'Completed' ? 'success' : order.status === 'In Progress' ? 'warning' : 'default'} />
                      </TableCell>
                      <TableCell align="right">
                        <IconButton size="small" onClick={() => setSelectedOrder(order)} title={t('common:views.viewDetails')}>
                          <VisibilityIcon />
                        </IconButton>
                      </TableCell>
                    </TableRow>
                  ))
                )}
              </TableBody>
            </Table>
          </TableContainer>
        </Paper>
      )}

      {showModal && (
        <CreateWorkOrderModal
          onClose={() => setShowModal(false)}
          onSuccess={() => {
            setShowModal(false)
            setReloadKey((k) => k + 1)
          }}
        />
      )}
      
      {selectedOrder && (
        <WorkOrderDetailDrawer order={selectedOrder} onClose={() => setSelectedOrder(null)} />
      )}
    </Box>
  )
}

export function InventoryView() {
  const { translate: t } = useI18n()
  return (
    <Box sx={{ p: 4, textAlign: 'center' }}>
      <Typography variant="h4" gutterBottom>{t('common:views.inventoryTitle')}</Typography>
      <Typography color="text.secondary">Inventory module will be implemented with MUI DataGrid.</Typography>
    </Box>
  )
}

export function PaymentsView() {
  const { translate: t } = useI18n()
  return (
    <Box sx={{ p: 4, textAlign: 'center' }}>
      <Typography variant="h4" gutterBottom>{t('common:views.paymentsTitle')}</Typography>
      <Typography color="text.secondary">Payments module will be implemented with MUI DataGrid.</Typography>
    </Box>
  )
}

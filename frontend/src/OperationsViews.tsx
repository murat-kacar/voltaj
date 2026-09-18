import { useEffect, useMemo, useState } from 'react'
import { Box, Typography, Button, TextField, InputAdornment, Paper, Table, TableBody, TableCell, TableContainer, TableHead, TableRow, Chip, IconButton, CircularProgress, Alert, Tabs, Tab } from '@mui/material'
import SearchIcon from '@mui/icons-material/Search'
import FilterListIcon from '@mui/icons-material/FilterList'
import AddIcon from '@mui/icons-material/Add'
import VisibilityIcon from '@mui/icons-material/Visibility'
import { workOrdersApi, auditLogsApi, type WorkOrder as ApiWorkOrder, type StockDto, type PaymentDto, type AuditLogDto } from './api'
import { CreateWorkOrderModal } from './CreateWorkOrderModal'
import { WorkOrderDetailDrawer } from './WorkOrderDetailDrawer'
import { useI18n } from './i18n'
import { DataGrid, type GridColDef } from '@mui/x-data-grid'

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
    [query, tab, sourceOrders, currentUserId]
  )

  return (
    <Box>
      <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 3 }}>
        <Box>
          <Typography variant="overline" color="text.secondary">{t('common:views.workOrdersLabel')}</Typography>
          <Typography variant="h4">{t('common:views.workOrdersTitle')}</Typography>
        </Box>
        <Button variant="contained" startIcon={<AddIcon />} onClick={() => setShowModal(true)}>
          {t('common:modals.newWorkOrder')}
        </Button>
      </Box>

      {loading && <Box sx={{ display: 'flex', justifyContent: 'center', p: 4 }}><CircularProgress /></Box>}
      {error && <Alert severity="error" sx={{ mb: 3 }} action={<Button color="inherit" size="small" onClick={() => setReloadKey(k => k+1)}>{t('common:common.retry')}</Button>}>{error}</Alert>}

      {!loading && !error && (
        <Paper sx={{ width: '100%', mb: 2 }}>
          <Tabs value={tab} onChange={(_, v) => setTab(v)} sx={{ borderBottom: 1, borderColor: 'divider', px: 2, pt: 1 }}>
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
              slotProps={{ input: { startAdornment: <InputAdornment position="start"><SearchIcon /></InputAdornment> } }}
              sx={{ flexGrow: 1, maxWidth: 400 }}
            />
            <Button variant="outlined" startIcon={<FilterListIcon />}>{t('common:fields.filter')}</Button>
          </Box>
          <TableContainer>
            <Table>
              <TableHead>
                <TableRow>
                  <TableCell>{t('common:fields.number')}</TableCell>
                  <TableCell>{t('common:fields.title')}</TableCell>
                  <TableCell>{t('common:fields.customer')}</TableCell>
                  <TableCell>{t('common:common.assigned')}</TableCell>
                  <TableCell>{t('common:fields.status')}</TableCell>
                  <TableCell align="right">{t('common:fields.actions')}</TableCell>
                </TableRow>
              </TableHead>
              <TableBody>
                {filtered.length === 0 ? (
                  <TableRow><TableCell colSpan={6} align="center">{t('common:common.noDataFound')}</TableCell></TableRow>
                ) : (
                  filtered.map((order) => (
                    <TableRow key={order.id} hover>
                      <TableCell><strong>{order.number}</strong></TableCell>
                      <TableCell>{order.title}</TableCell>
                      <TableCell>{order.customerId}</TableCell>
                      <TableCell>{order.assignedUserId || <Typography variant="caption" color="text.secondary">{t('common:common.unassigned')}</Typography>}</TableCell>
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
  const rows: StockDto[] = []
  const loading = false

  const columns: GridColDef[] = [
    { field: 'materialCode', headerName: 'Code', width: 150 },
    { field: 'name', headerName: 'Name', flex: 1 },
    { field: 'quantityOnHand', headerName: 'On Hand', width: 120, type: 'number' },
    { field: 'reservedQuantity', headerName: 'Reserved', width: 120, type: 'number' },
    { field: 'availableQuantity', headerName: 'Available', width: 120, type: 'number' },
  ]

  return (
    <Box>
      <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 3 }}>
        <Box>
          <Typography variant="overline" color="text.secondary">{t('common:views.inventoryTitle')}</Typography>
          <Typography variant="h4">{t('common:views.inventoryTitle')}</Typography>
        </Box>
      </Box>
      <Paper sx={{ width: '100%', height: 400, mb: 2 }}>
        <DataGrid
          rows={rows}
          columns={columns}
          loading={loading}
          getRowId={(row) => row.materialCode}
          initialState={{ pagination: { paginationModel: { pageSize: 10 } } }}
          pageSizeOptions={[10, 25, 50]}
          disableRowSelectionOnClick
        />
      </Paper>
    </Box>
  )
}

export function PaymentsView() {
  const { translate: t } = useI18n()
  const rows: PaymentDto[] = []
  const loading = false

  const columns: GridColDef[] = [
    { field: 'id', headerName: 'ID', width: 90 },
    { field: 'customerId', headerName: 'Customer ID', width: 300 },
    { field: 'amount', headerName: 'Amount', width: 130, type: 'number' },
    { field: 'paymentMethod', headerName: 'Method', width: 150 },
    { field: 'paymentDate', headerName: 'Date', width: 200, type: 'dateTime', valueGetter: (val) => new Date(val) },
  ]

  return (
    <Box>
      <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 3 }}>
        <Box>
          <Typography variant="overline" color="text.secondary">{t('common:views.paymentsTitle')}</Typography>
          <Typography variant="h4">{t('common:views.paymentsTitle')}</Typography>
        </Box>
      </Box>
      <Paper sx={{ width: '100%', height: 400, mb: 2 }}>
        <DataGrid
          rows={rows}
          columns={columns}
          loading={loading}
          initialState={{ pagination: { paginationModel: { pageSize: 10 } } }}
          pageSizeOptions={[10, 25, 50]}
          disableRowSelectionOnClick
        />
      </Paper>
    </Box>
  )
}

export function AuditLogsView() {
  const { translate: t } = useI18n()
  const [rows, setRows] = useState<AuditLogDto[]>([])
  const [loading, setLoading] = useState(true)

  useEffect(() => {
    let ignore = false
    auditLogsApi.listRecent()
      .then(items => { if (!ignore) { setRows(items); setLoading(false); } })
      .catch(() => { if (!ignore) setLoading(false); })
    return () => { ignore = true }
  }, [])

  const columns: GridColDef[] = [
    { field: 'timestamp', headerName: 'Time', width: 180, type: 'dateTime', valueGetter: (val) => new Date(val) },
    { field: 'action', headerName: 'Action', width: 200 },
    { field: 'entityName', headerName: 'Entity', width: 150 },
    { field: 'entityId', headerName: 'Entity ID', width: 250 },
    { field: 'details', headerName: 'Details', flex: 1 },
  ]

  return (
    <Box>
      <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 3 }}>
        <Box>
          <Typography variant="overline" color="text.secondary">{t('common:nav.system')}</Typography>
          <Typography variant="h4">{t('common:nav.auditLog')}</Typography>
        </Box>
      </Box>
      <Paper sx={{ width: '100%', height: 600, mb: 2 }}>
        <DataGrid
          rows={rows}
          columns={columns}
          loading={loading}
          initialState={{ pagination: { paginationModel: { pageSize: 15 } } }}
          pageSizeOptions={[15, 50, 100]}
          disableRowSelectionOnClick
        />
      </Paper>
    </Box>
  )
}

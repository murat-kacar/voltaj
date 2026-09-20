import { useEffect, useMemo, useState } from 'react'
import { Box, Typography, Button, TextField, InputAdornment, Paper, Table, TableBody, TableCell, TableContainer, TableHead, TableRow, Chip, IconButton, CircularProgress, Alert, Tabs, Tab, Dialog, DialogTitle, DialogContent, DialogActions, MenuItem, Select, FormControl, InputLabel } from '@mui/material'
import SearchIcon from '@mui/icons-material/Search'
import FilterListIcon from '@mui/icons-material/FilterList'
import AddIcon from '@mui/icons-material/Add'
import VisibilityIcon from '@mui/icons-material/Visibility'
import { workOrdersApi, auditLogsApi, paymentsApi, type WorkOrder as ApiWorkOrder, type StockDto, type PaymentDto, type SalesInvoiceDto, type AuditLogDto, type Customer } from './api'
import { CreateWorkOrderModal } from './CreateWorkOrderModal'
import { WorkOrderDetailDrawer } from './WorkOrderDetailDrawer'
import { CustomerPicker } from './customers/CustomerPicker'
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
      .then((page) => {
        if (!ignore) {
          setRawOrders(page.items)
          setError('')
          setLoading(false)
        }
      })
      .catch((reason: unknown) => {
        if (!ignore) {
          setError(reason instanceof Error ? reason.message : t('workOrders:errors.loadFailed'))
          setLoading(false)
        }
      })
    return () => {
      ignore = true
    }
  }, [reloadKey, t])

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
          <Typography variant="overline" color="text.secondary">{t('workOrders:eyebrow')}</Typography>
          <Typography variant="h4">{t('workOrders:title')}</Typography>
        </Box>
        <Button variant="contained" startIcon={<AddIcon />} onClick={() => setShowModal(true)}>
          {t('workOrders:newOrder')}
        </Button>
      </Box>

      {loading && <Box sx={{ display: 'flex', justifyContent: 'center', p: 4 }}><CircularProgress /></Box>}
      {error && <Alert severity="error" sx={{ mb: 3 }} action={<Button color="inherit" size="small" onClick={() => setReloadKey(k => k+1)}>{t('common:common.retry')}</Button>}>{error}</Alert>}

      {!loading && !error && (
        <Paper sx={{ width: '100%', mb: 2 }}>
          <Tabs value={tab} onChange={(_, v) => setTab(v)} sx={{ borderBottom: 1, borderColor: 'divider', px: 2, pt: 1 }}>
            <Tab label={t('workOrders:tabs.all')} value="All" />
            <Tab label={t('workOrders:tabs.mine')} value="Mine" />
            <Tab label={t('workOrders:tabs.unassigned')} value="Unassigned" />
          </Tabs>

          <Box sx={{ p: 2, display: 'flex', gap: 2, alignItems: 'center' }}>
            <TextField
              size="small"
              placeholder={t('workOrders:searchPlaceholder')}
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
                  <TableCell>{t('workOrders:table.order')}</TableCell>
                  <TableCell>{t('common:fields.title')}</TableCell>
                  <TableCell>{t('workOrders:table.customer')}</TableCell>
                  <TableCell>{t('workOrders:table.assigned')}</TableCell>
                  <TableCell>{t('workOrders:table.status')}</TableCell>
                  <TableCell align="right">{t('common:fields.actions')}</TableCell>
                </TableRow>
              </TableHead>
              <TableBody className="module-table">
                {filtered.length === 0 ? (
                  <TableRow><TableCell colSpan={6} align="center">{t('common:common.noDataFound')}</TableCell></TableRow>
                ) : (
                  filtered.map((order) => (
                    <TableRow key={order.id} hover className="work-module-row" onClick={() => setSelectedOrder(order)} sx={{ cursor: 'pointer' }}>
                      <TableCell><strong>{order.number}</strong></TableCell>
                      <TableCell>{order.title}</TableCell>
                      <TableCell>{order.customerId}</TableCell>
                      <TableCell>{order.assignedUserId || <Typography variant="caption" color="text.secondary">{t('workOrders:drawer.unassigned')}</Typography>}</TableCell>
                      <TableCell>
                        <Chip label={order.status} size="small"
                          color={order.status === 'Completed' ? 'success' : order.status === 'InProgress' ? 'warning' : order.status === 'Cancelled' ? 'error' : 'default'} />
                      </TableCell>
                      <TableCell align="right">
                        <IconButton size="small" onClick={(e) => { e.stopPropagation(); setSelectedOrder(order) }} title={t('common:views.viewDetails')}>
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
        <WorkOrderDetailDrawer
          order={selectedOrder}
          onClose={() => setSelectedOrder(null)}
          onUpdated={(updated) => {
            setSelectedOrder(updated)
            setRawOrders((prev) => prev ? prev.map((o) => o.id === updated.id ? updated : o) : prev)
          }}
        />
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
  const [customer, setCustomer] = useState<Customer | null>(null)
  const [tab, setTab] = useState(0)
  const [invoices, setInvoices] = useState<SalesInvoiceDto[]>([])
  const [payments, setPayments] = useState<PaymentDto[]>([])
  const [loadingData, setLoadingData] = useState(false)
  const [dataError, setDataError] = useState('')
  const [showRecordPayment, setShowRecordPayment] = useState(false)
  const [payAmount, setPayAmount] = useState('')
  const [payMethod, setPayMethod] = useState('Cash')
  const [payDate, setPayDate] = useState(new Date().toISOString().slice(0, 10))
  const [saving, setSaving] = useState(false)
  const [saveError, setSaveError] = useState('')
  const [reload, setReload] = useState(0)

  useEffect(() => {
    if (!customer) return
    let ignore = false
    Promise.all([
      paymentsApi.listInvoicesByCustomer(customer.id),
      paymentsApi.listByCustomer(customer.id),
    ])
      .then(([inv, pay]) => {
        if (!ignore) {
          setInvoices(inv.items)
          setPayments(pay.items)
        }
      })
      .catch(() => { if (!ignore) setDataError(t('payments:errors.loadFailed')) })
      .finally(() => { if (!ignore) setLoadingData(false) })
    return () => { ignore = true }
  }, [customer, reload, t])

  const invoiceColumns: GridColDef[] = [
    { field: 'invoiceNumber', headerName: t('payments:table.invoiceNumber'), width: 150 },
    { field: 'grandTotal', headerName: t('payments:table.grandTotal'), width: 130, type: 'number' },
    { field: 'paidAmount', headerName: t('payments:table.paid'), width: 130, type: 'number' },
    { field: 'remainingAmount', headerName: t('payments:table.remaining'), width: 130, type: 'number' },
    { field: 'invoiceDate', headerName: t('payments:table.invoiceDate'), width: 150 },
  ]

  const paymentColumns: GridColDef[] = [
    { field: 'amount', headerName: t('payments:table.amount'), width: 130, type: 'number' },
    { field: 'paymentMethod', headerName: t('payments:table.method'), width: 150 },
    { field: 'paymentDate', headerName: t('payments:table.paymentDate'), width: 150 },
  ]

  const handleRecordPayment = async () => {
    if (!customer) return
    const amount = parseFloat(payAmount)
    if (isNaN(amount) || amount <= 0) { setSaveError('Amount must be greater than zero.'); return }
    setSaving(true)
    setSaveError('')
    try {
      await paymentsApi.create({ customerId: customer.id, amount, paymentMethod: payMethod, paymentDate: payDate })
      setShowRecordPayment(false)
      setPayAmount('')
      setLoadingData(true); setDataError('')
      setReload(r => r + 1)
    } catch (err) {
      setSaveError(err instanceof Error ? err.message : t('payments:errors.createFailed'))
    } finally {
      setSaving(false)
    }
  }

  return (
    <Box>
      <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 3 }}>
        <Box>
          <Typography variant="overline" color="text.secondary">{t('payments:eyebrow')}</Typography>
          <Typography variant="h4">{t('payments:title')}</Typography>
          <Typography variant="body2" color="text.secondary">{t('payments:subtitle')}</Typography>
        </Box>
        {customer && tab === 1 && (
          <Button variant="contained" startIcon={<AddIcon />} onClick={() => setShowRecordPayment(true)}>
            {t('payments:actions.recordPayment')}
          </Button>
        )}
      </Box>
      <Box sx={{ mb: 2 }}>
        <CustomerPicker
          value={customer}
          onChange={(c) => { setLoadingData(true); setDataError(''); setCustomer(c); setTab(0) }}
          label={t('workOrders:form.selectCustomer')}
        />
      </Box>
      {!customer && (
        <Alert severity="info">{t('payments:selectCustomerPrompt')}</Alert>
      )}
      {customer && (
        <>
          {dataError && <Alert severity="error" sx={{ mb: 2 }}>{dataError}</Alert>}
          <Tabs value={tab} onChange={(_, v: number) => setTab(v)} sx={{ mb: 2 }}>
            <Tab label={t('payments:tabs.invoices')} />
            <Tab label={t('payments:tabs.payments')} />
          </Tabs>
          {tab === 0 && (
            <Paper sx={{ width: '100%', height: 400 }}>
              <DataGrid
                rows={invoices}
                columns={invoiceColumns}
                loading={loadingData}
                initialState={{ pagination: { paginationModel: { pageSize: 10 } } }}
                pageSizeOptions={[10, 25, 50]}
                disableRowSelectionOnClick
              />
            </Paper>
          )}
          {tab === 1 && (
            <Paper sx={{ width: '100%', height: 400 }}>
              <DataGrid
                rows={payments}
                columns={paymentColumns}
                loading={loadingData}
                initialState={{ pagination: { paginationModel: { pageSize: 10 } } }}
                pageSizeOptions={[10, 25, 50]}
                disableRowSelectionOnClick
              />
            </Paper>
          )}
        </>
      )}
      <Dialog open={showRecordPayment} onClose={() => setShowRecordPayment(false)} maxWidth="xs" fullWidth>
        <DialogTitle>{t('payments:form.title')}</DialogTitle>
        <DialogContent sx={{ display: 'flex', flexDirection: 'column', gap: 2, pt: '16px !important' }}>
          {saveError && <Alert severity="error">{saveError}</Alert>}
          <TextField
            label={t('payments:form.amount')}
            type="number"
            value={payAmount}
            onChange={(e) => setPayAmount(e.target.value)}
            fullWidth
            size="small"
          />
          <FormControl fullWidth size="small">
            <InputLabel>{t('payments:form.method')}</InputLabel>
            <Select value={payMethod} onChange={(e) => setPayMethod(e.target.value)} label={t('payments:form.method')}>
              <MenuItem value="Cash">{t('common:sales.methods.Cash')}</MenuItem>
              <MenuItem value="Card">{t('common:sales.methods.Card')}</MenuItem>
              <MenuItem value="BankTransfer">{t('common:sales.methods.BankTransfer')}</MenuItem>
            </Select>
          </FormControl>
          <TextField
            label={t('payments:form.date')}
            type="date"
            value={payDate}
            onChange={(e) => setPayDate(e.target.value)}
            fullWidth
            size="small"
            slotProps={{ inputLabel: { shrink: true } }}
          />
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setShowRecordPayment(false)}>{t('common:actions.cancel')}</Button>
          <Button variant="contained" onClick={handleRecordPayment} disabled={saving}>
            {saving ? t('payments:form.submitting') : t('payments:form.submit')}
          </Button>
        </DialogActions>
      </Dialog>
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

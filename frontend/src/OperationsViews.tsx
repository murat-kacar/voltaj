import { useEffect, useMemo, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { Box, Typography, Button, TextField, InputAdornment, Paper, Table, TableBody, TableCell, TableContainer, TableHead, TableRow, Chip, IconButton, CircularProgress, Alert, MenuItem } from '@mui/material'
import SearchIcon from '@mui/icons-material/Search'
import AddIcon from '@mui/icons-material/Add'
import VisibilityIcon from '@mui/icons-material/Visibility'
import { workOrdersApi, customersApi, usersApi, auditLogsApi, type WorkOrder as ApiWorkOrder, type AuditLogDto, type Customer, type UserSummary } from './api'
import { CreateWorkOrderModal } from './CreateWorkOrderModal'
import { useI18n } from './i18n'
import { DataGrid, type GridColDef } from '@mui/x-data-grid'

const STATUS_OPTIONS = ['Open', 'Assigned', 'EnRoute', 'InProgress', 'OnHold', 'Completed', 'ReadyForBilling', 'Invoiced', 'Cancelled', 'NoShow']

function statusColor(status: string): 'default' | 'primary' | 'secondary' | 'error' | 'info' | 'success' | 'warning' {
  switch (status) {
    case 'InProgress': return 'warning'
    case 'Completed': return 'success'
    case 'ReadyForBilling': return 'info'
    case 'Invoiced': return 'success'
    case 'Cancelled': return 'error'
    case 'NoShow': return 'error'
    case 'OnHold': return 'warning'
    case 'Assigned': return 'primary'
    case 'EnRoute': return 'primary'
    default: return 'default'
  }
}

export function WorkOrdersView() {
  const { translate: t } = useI18n()
  const navigate = useNavigate()
  const sessionData = localStorage.getItem('voltflow.session')
  const currentUserId = sessionData ? JSON.parse(sessionData).userId : null

  const [ownerFilter, setOwnerFilter] = useState('All')
  const [statusFilter, setStatusFilter] = useState('All')
  const [query, setQuery] = useState('')
  const [rawOrders, setRawOrders] = useState<ApiWorkOrder[] | null>(null)
  const [customerMap, setCustomerMap] = useState<Record<string, string>>({})
  const [userMap, setUserMap] = useState<Record<string, string>>({})
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')
  const [showModal, setShowModal] = useState(false)
  const [reloadKey, setReloadKey] = useState(0)

  useEffect(() => {
    let ignore = false
    Promise.all([
      workOrdersApi.list(),
      customersApi.list(),
      usersApi.page({ approved: true, limit: 200 }),
    ])
      .then(([page, customers, usersPage]) => {
        if (ignore) return
        setRawOrders(page.items)
        const cm: Record<string, string> = {}
        customers.forEach((c: Customer) => { cm[c.id] = c.fullName })
        setCustomerMap(cm)
        const um: Record<string, string> = {}
        usersPage.items.forEach((u: UserSummary) => { um[u.id] = u.name })
        setUserMap(um)
        setError('')
        setLoading(false)
      })
      .catch((reason: unknown) => {
        if (ignore) return
        setError(reason instanceof Error ? reason.message : t('workOrders:errors.loadFailed'))
        setLoading(false)
      })
    return () => { ignore = true }
  }, [reloadKey, t])

  const sourceOrders = useMemo(() => rawOrders ?? [], [rawOrders])
  const filtered = useMemo(
    () =>
      sourceOrders
        .filter((order) =>
          ownerFilter === 'Mine'
            ? order.assignedUserId === currentUserId
            : ownerFilter === 'Unassigned'
            ? !order.assignedUserId
            : true
        )
        .filter((order) =>
          statusFilter === 'All' ? true : order.status === statusFilter
        )
        .filter((order) => {
          if (!query) return true
          const customerName = customerMap[order.customerId] ?? ''
          return `${order.number} ${order.title} ${customerName}`.toLowerCase().includes(query.toLowerCase())
        }),
    [query, ownerFilter, statusFilter, sourceOrders, currentUserId, customerMap]
  )

  const statusLabel = (status: string): string => {
    const key = `workOrders:filter.status${status}`
    const translated = (t as unknown as (k: string) => string)(key)
    return translated !== key ? translated : status
  }

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
      {error && <Alert severity="error" sx={{ mb: 3 }} action={<Button color="inherit" size="small" onClick={() => setReloadKey(k => k + 1)}>{t('common:common.retry')}</Button>}>{error}</Alert>}

      {!loading && !error && (
        <Paper sx={{ width: '100%', mb: 2 }}>
          <Box sx={{ p: 2, display: 'flex', gap: 2, alignItems: 'center', flexWrap: 'wrap' }}>
            <TextField
              select
              size="small"
              label={t('workOrders:filter.label')}
              value={ownerFilter}
              onChange={(e) => setOwnerFilter(e.target.value)}
              sx={{ minWidth: 160 }}
              data-testid="work-orders-owner-filter"
            >
              <MenuItem value="All">{t('workOrders:filter.all')}</MenuItem>
              <MenuItem value="Mine">{t('workOrders:filter.mine')}</MenuItem>
              <MenuItem value="Unassigned">{t('workOrders:filter.unassigned')}</MenuItem>
            </TextField>
            <TextField
              select
              size="small"
              label={t('workOrders:filter.statusLabel')}
              value={statusFilter}
              onChange={(e) => setStatusFilter(e.target.value)}
              sx={{ minWidth: 200 }}
              data-testid="work-orders-status-filter"
            >
              <MenuItem value="All">{t('workOrders:filter.statusAll')}</MenuItem>
              {STATUS_OPTIONS.map((s) => (
                <MenuItem key={s} value={s}>{statusLabel(s)}</MenuItem>
              ))}
            </TextField>
            <TextField
              size="small"
              placeholder={t('workOrders:searchPlaceholder')}
              value={query}
              onChange={(e) => setQuery(e.target.value)}
              slotProps={{ input: { startAdornment: <InputAdornment position="start"><SearchIcon /></InputAdornment> } }}
              sx={{ flexGrow: 1, maxWidth: 400 }}
            />
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
                    <TableRow key={order.id} hover className="work-module-row" onClick={() => navigate('/work-orders/' + order.id)} sx={{ cursor: 'pointer' }}>
                      <TableCell><strong>{order.number}</strong></TableCell>
                      <TableCell>{order.title}</TableCell>
                      <TableCell>{customerMap[order.customerId] ?? order.customerId}</TableCell>
                      <TableCell>
                        {order.assignedUserId
                          ? (userMap[order.assignedUserId] ?? order.assignedUserId)
                          : <Typography variant="caption" color="text.secondary">{t('workOrders:drawer.unassigned')}</Typography>}
                      </TableCell>
                      <TableCell>
                        <Chip
                          label={statusLabel(order.status)}
                          size="small"
                          color={statusColor(order.status)}
                        />
                      </TableCell>
                      <TableCell align="right">
                        <IconButton size="small" onClick={(e) => { e.stopPropagation(); navigate('/work-orders/' + order.id) }} title={t('common:views.viewDetails')}>
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
    </Box>
  )
}

/** The record of what was done, newest first; the Settings page gives it its heading. */
export function AuditLogPanel() {
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
    { field: 'timestamp', headerName: t('common:settings.auditLog.time'), width: 180, type: 'dateTime', valueGetter: (val) => new Date(val) },
    { field: 'action', headerName: t('common:settings.auditLog.action'), width: 200 },
    { field: 'entityName', headerName: t('common:settings.auditLog.entity'), width: 150 },
    { field: 'entityId', headerName: t('common:settings.auditLog.entityId'), width: 250 },
    { field: 'details', headerName: t('common:settings.auditLog.details'), flex: 1 },
  ]

  return (
    <Paper sx={{ width: '100%', height: 600 }}>
      <DataGrid
        rows={rows}
        columns={columns}
        loading={loading}
        initialState={{ pagination: { paginationModel: { pageSize: 15 } } }}
        pageSizeOptions={[15, 50, 100]}
        disableRowSelectionOnClick
      />
    </Paper>
  )
}

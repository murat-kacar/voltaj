import { useEffect, useMemo, useState } from 'react'
import { Box, Typography, Button, TextField, InputAdornment, Paper, Table, TableBody, TableCell, TableContainer, TableHead, TableRow, Chip, IconButton, CircularProgress, Alert, MenuItem } from '@mui/material'
import SearchIcon from '@mui/icons-material/Search'
import FilterListIcon from '@mui/icons-material/FilterList'
import AddIcon from '@mui/icons-material/Add'
import VisibilityIcon from '@mui/icons-material/Visibility'
import { workOrdersApi, auditLogsApi, type WorkOrder as ApiWorkOrder, type AuditLogDto } from './api'
import { CreateWorkOrderModal } from './CreateWorkOrderModal'
import { WorkOrderDetailDrawer } from './WorkOrderDetailDrawer'
import { useI18n } from './i18n'
import { DataGrid, type GridColDef } from '@mui/x-data-grid'

export function WorkOrdersView() {
  const { translate: t } = useI18n()
  const sessionData = localStorage.getItem('voltflow.session')
  const currentUserId = sessionData ? JSON.parse(sessionData).userId : null

  const [ownerFilter, setOwnerFilter] = useState('All')
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
          ownerFilter === 'Mine'
            ? order.assignedUserId === currentUserId
            : ownerFilter === 'Unassigned'
            ? !order.assignedUserId
            : true
        )
        .filter((order) =>
          `${order.number} ${order.title} ${order.customerId}`.toLowerCase().includes(query.toLowerCase())
        ),
    [query, ownerFilter, sourceOrders, currentUserId]
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
          <Box sx={{ p: 2, display: 'flex', gap: 2, alignItems: 'center', flexWrap: 'wrap' }}>
            <TextField
              select
              size="small"
              label={t('workOrders:filter.label')}
              value={ownerFilter}
              onChange={(e) => setOwnerFilter(e.target.value)}
              sx={{ minWidth: 200 }}
              data-testid="work-orders-owner-filter"
            >
              <MenuItem value="All">{t('workOrders:filter.all')}</MenuItem>
              <MenuItem value="Mine">{t('workOrders:filter.mine')}</MenuItem>
              <MenuItem value="Unassigned">{t('workOrders:filter.unassigned')}</MenuItem>
            </TextField>
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

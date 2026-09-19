import { useEffect, useMemo, useState } from 'react'
import {
  Alert,
  Box,
  Button,
  Chip,
  CircularProgress,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  InputAdornment,
  MenuItem,
  Paper,
  Stack,
  TextField,
  ToggleButton,
  ToggleButtonGroup,
  Typography,
} from '@mui/material'
import SearchIcon from '@mui/icons-material/Search'
import PrintIcon from '@mui/icons-material/Print'
import { DataGrid, type GridColDef, type GridPaginationModel } from '@mui/x-data-grid'
import { quickSalesApi, type PaymentMethod, type QuickSale, type QuickSaleSummary, type SaleLine } from './api'
import { AmountField } from './AmountField'
import { Receipt } from './Receipt'
import { formatDate } from './i18n/formatters'
import { useI18n } from './i18n'
import { errorText, formatMoney, gridLocaleText, rangeStart, saleStatusKey, statusColor } from './quickSaleUtils'
import { divRound, fromCents, toCents } from './saleMath'

type Range = 'today' | 'week' | 'all'

type Props = {
  isManager: boolean
  /** Something about the sales changed (a void or a return), so the shift figures are stale. */
  onChanged: () => void
}

/** Every sale that was rung up, searchable, with the receipt to look at again and, for managers, void and return. */
export function HistoryTab({ isManager, onChanged }: Props) {
  const { translate: t, lang } = useI18n()
  const [range, setRange] = useState<Range>('today')
  const [status, setStatus] = useState('')
  const [search, setSearch] = useState('')
  const [paging, setPaging] = useState<GridPaginationModel>({ page: 0, pageSize: 25 })
  const [rows, setRows] = useState<QuickSaleSummary[]>([])
  const [total, setTotal] = useState(0)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')
  const [selectedId, setSelectedId] = useState<string | null>(null)
  const [reloadKey, setReloadKey] = useState(0)

  useEffect(() => {
    let ignore = false
    const handle = window.setTimeout(() => {
      setLoading(true)
      quickSalesApi
        .list({
          search: search.trim() || undefined,
          status: status || undefined,
          from: rangeStart(range),
          limit: paging.pageSize,
          offset: paging.page * paging.pageSize,
        })
        .then((page) => {
          if (ignore) return
          setRows(page.items)
          setTotal(page.total)
          setError('')
        })
        .catch((reason: unknown) => {
          if (!ignore) setError(errorText(reason))
        })
        .finally(() => {
          if (!ignore) setLoading(false)
        })
    }, 250)
    return () => {
      ignore = true
      window.clearTimeout(handle)
    }
  }, [search, status, range, paging, reloadKey])

  const columns = useMemo<GridColDef<QuickSaleSummary>[]>(() => {
    const base = { sortable: false, filterable: false }
    return [
      { ...base, field: 'saleNumber', headerName: t('common:sales.history.columns.number'), width: 130 },
      {
        ...base,
        field: 'soldAt',
        headerName: t('common:sales.history.columns.date'),
        width: 170,
        renderCell: (params) => formatDate(params.row.soldAt, lang),
      },
      { ...base, field: 'cashierName', headerName: t('common:sales.history.columns.cashier'), width: 150 },
      {
        ...base,
        field: 'paymentMethods',
        headerName: t('common:sales.history.columns.payment'),
        flex: 1,
        minWidth: 140,
        renderCell: (params) => params.row.paymentMethods.map((method) => t(`common:sales.methods.${method}`)).join(' + '),
      },
      {
        ...base,
        field: 'grandTotal',
        headerName: t('common:sales.history.columns.total'),
        width: 130,
        align: 'right',
        headerAlign: 'right',
        renderCell: (params) => formatMoney(params.row.grandTotal, lang),
      },
      {
        ...base,
        field: 'status',
        headerName: t('common:sales.history.columns.status'),
        width: 130,
        renderCell: (params) => {
          const key = saleStatusKey(params.row.status, params.row.hasReturns)
          return <Chip size="small" color={statusColor(key)} label={t(`common:sales.status.${key}`)} />
        },
      },
    ]
  }, [t, lang])

  const changeFilter = (apply: () => void) => {
    apply()
    setPaging((current) => ({ ...current, page: 0 }))
  }

  return (
    <Box>
      <Stack direction={{ xs: 'column', md: 'row' }} spacing={2} sx={{ mb: 2 }}>
        <ToggleButtonGroup
          size="small"
          exclusive
          value={range}
          onChange={(_, value: Range | null) => value && changeFilter(() => setRange(value))}
        >
          <ToggleButton value="today">{t('common:sales.history.range.today')}</ToggleButton>
          <ToggleButton value="week">{t('common:sales.history.range.week')}</ToggleButton>
          <ToggleButton value="all">{t('common:sales.history.range.all')}</ToggleButton>
        </ToggleButtonGroup>
        <TextField
          size="small"
          select
          value={status}
          onChange={(event) => changeFilter(() => setStatus(event.target.value))}
          sx={{ minWidth: 180 }}
          slotProps={{ select: { displayEmpty: true } }}
        >
          <MenuItem value="">{t('common:sales.history.statusAll')}</MenuItem>
          <MenuItem value="Completed">{t('common:sales.status.Completed')}</MenuItem>
          <MenuItem value="Voided">{t('common:sales.status.Voided')}</MenuItem>
        </TextField>
        <TextField
          size="small"
          placeholder={t('common:sales.history.search')}
          value={search}
          onChange={(event) => changeFilter(() => setSearch(event.target.value))}
          slotProps={{ input: { startAdornment: <InputAdornment position="start"><SearchIcon /></InputAdornment> } }}
        />
      </Stack>

      {error && <Alert severity="error" sx={{ mb: 2 }}>{error}</Alert>}

      <Paper sx={{ width: '100%' }}>
        <DataGrid
          rows={rows}
          columns={columns}
          loading={loading}
          paginationMode="server"
          rowCount={total}
          paginationModel={paging}
          onPaginationModelChange={setPaging}
          pageSizeOptions={[25, 50, 100]}
          onRowClick={(params) => setSelectedId(params.row.id)}
          disableRowSelectionOnClick
          disableColumnFilter
          disableColumnMenu
          autoHeight
          localeText={gridLocaleText(lang, t('common:sales.history.empty'))}
          sx={{ '& .MuiDataGrid-row': { cursor: 'pointer' } }}
        />
      </Paper>

      {selectedId && (
        <SaleDetailDialog
          id={selectedId}
          isManager={isManager}
          onClose={() => setSelectedId(null)}
          onChanged={() => {
            setReloadKey((key) => key + 1)
            onChanged()
          }}
        />
      )}
    </Box>
  )
}

function SaleDetailDialog({ id, isManager, onClose, onChanged }: { id: string; isManager: boolean; onClose: () => void; onChanged: () => void }) {
  const { translate: t } = useI18n()
  const [sale, setSale] = useState<QuickSale | null>(null)
  const [error, setError] = useState('')
  const [voidOpen, setVoidOpen] = useState(false)
  const [returnOpen, setReturnOpen] = useState(false)

  useEffect(() => {
    let ignore = false
    quickSalesApi
      .get(id)
      .then((loaded) => {
        if (!ignore) setSale(loaded)
      })
      .catch((reason: unknown) => {
        if (!ignore) setError(errorText(reason))
      })
    return () => {
      ignore = true
    }
  }, [id])

  const completed = sale?.status === 'Completed'
  const hasReturns = (sale?.returns.length ?? 0) > 0
  const canReturn = completed && (sale?.lines.some((line) => remaining(line) > 0) ?? false)

  const changed = (updated: QuickSale) => {
    setSale(updated)
    onChanged()
  }

  return (
    <Dialog open onClose={onClose} maxWidth="xs" fullWidth>
      <DialogTitle>{t('common:sales.detail.title')}</DialogTitle>
      <DialogContent>
        {!sale && !error && <Box sx={{ display: 'flex', justifyContent: 'center', p: 3 }}><CircularProgress /></Box>}
        {error && <Alert severity="error">{error}</Alert>}
        {sale && (
          <>
            <Receipt sale={sale} />
            {sale.status === 'Voided' && (
              <Alert severity="error" sx={{ mt: 2 }}>
                {t('common:sales.detail.voidedInfo', { date: formatDate(sale.voidedAt ?? sale.soldAt), reason: sale.voidReason ?? '' })}
              </Alert>
            )}
            {isManager && completed && !hasReturns && (
              <Typography variant="caption" color="text.secondary" sx={{ display: 'block', mt: 2 }}>{t('common:sales.detail.voidHint')}</Typography>
            )}
            {!isManager && completed && (
              <Typography variant="caption" color="text.secondary" sx={{ display: 'block', mt: 2 }}>{t('common:sales.detail.managerOnly')}</Typography>
            )}
          </>
        )}
      </DialogContent>
      <DialogActions sx={{ flexWrap: 'wrap' }}>
        {sale && <Button startIcon={<PrintIcon />} onClick={() => window.print()}>{t('common:sales.pos.print')}</Button>}
        {isManager && completed && !hasReturns && <Button color="error" onClick={() => setVoidOpen(true)}>{t('common:sales.detail.void')}</Button>}
        {isManager && canReturn && <Button onClick={() => setReturnOpen(true)}>{t('common:sales.detail.return')}</Button>}
        <Button variant="contained" onClick={onClose}>{t('common:actions.close')}</Button>
      </DialogActions>

      {sale && voidOpen && <VoidDialog sale={sale} onClose={() => setVoidOpen(false)} onDone={(updated) => { setVoidOpen(false); changed(updated) }} />}
      {sale && returnOpen && <ReturnDialog sale={sale} onClose={() => setReturnOpen(false)} onDone={(updated) => { setReturnOpen(false); changed(updated) }} />}
    </Dialog>
  )
}

const remaining = (line: SaleLine): number => Math.round((line.quantity - line.returnedQuantity) * 100) / 100

function VoidDialog({ sale, onClose, onDone }: { sale: QuickSale; onClose: () => void; onDone: (sale: QuickSale) => void }) {
  const { translate: t } = useI18n()
  const [reason, setReason] = useState('')
  const [busy, setBusy] = useState(false)
  const [error, setError] = useState('')

  const submit = async () => {
    setBusy(true)
    setError('')
    try {
      onDone(await quickSalesApi.void(sale.id, reason.trim()))
    } catch (failure) {
      setError(errorText(failure))
      setBusy(false)
    }
  }

  return (
    <Dialog open onClose={onClose} maxWidth="xs" fullWidth>
      <DialogTitle>{`${t('common:sales.detail.void')} · ${sale.saleNumber}`}</DialogTitle>
      <DialogContent>
        <Stack spacing={2} sx={{ mt: 1 }}>
          <Typography variant="body2">{t('common:sales.detail.voidPrompt')}</Typography>
          <TextField autoFocus required label={t('common:sales.detail.reason')} value={reason} onChange={(event) => setReason(event.target.value)} />
          {error && <Alert severity="error">{error}</Alert>}
        </Stack>
      </DialogContent>
      <DialogActions>
        <Button onClick={onClose} disabled={busy}>{t('common:actions.cancel')}</Button>
        <Button color="error" variant="contained" disabled={busy || reason.trim() === ''} onClick={() => void submit()}>
          {busy ? <CircularProgress size={22} /> : t('common:sales.detail.void')}
        </Button>
      </DialogActions>
    </Dialog>
  )
}

function ReturnDialog({ sale, onClose, onDone }: { sale: QuickSale; onClose: () => void; onDone: (sale: QuickSale) => void }) {
  const { translate: t, lang } = useI18n()
  const [quantities, setQuantities] = useState<Record<string, number>>({})
  const [method, setMethod] = useState<PaymentMethod>('Cash')
  const [reason, setReason] = useState('')
  const [busy, setBusy] = useState(false)
  const [error, setError] = useState('')

  // the same cumulative rounding as the API, so the refund shown is the refund paid
  const refundCents = (line: SaleLine, quantity: number): number => {
    const sold = Math.round(line.quantity * 100)
    const before = Math.round(line.returnedQuantity * 100)
    const after = before + Math.round(quantity * 100)
    return divRound(toCents(line.lineTotal) * after, sold) - divRound(toCents(line.lineTotal) * before, sold)
  }

  const chosen = sale.lines.filter((line) => (quantities[line.id] ?? 0) > 0)
  const invalid = chosen.some((line) => (quantities[line.id] ?? 0) > remaining(line) || Math.round((quantities[line.id] ?? 0) * 100) / 100 !== quantities[line.id])
  const refund = chosen.reduce((sum, line) => sum + refundCents(line, quantities[line.id] ?? 0), 0)

  const submit = async () => {
    setBusy(true)
    setError('')
    try {
      onDone(await quickSalesApi.return(sale.id, {
        reason: reason.trim(),
        refundMethod: method,
        items: chosen.map((line) => ({ lineId: line.id, quantity: quantities[line.id] })),
      }))
    } catch (failure) {
      setError(errorText(failure))
      setBusy(false)
    }
  }

  return (
    <Dialog open onClose={onClose} maxWidth="xs" fullWidth>
      <DialogTitle>{`${t('common:sales.detail.return')} · ${sale.saleNumber}`}</DialogTitle>
      <DialogContent>
        <Stack spacing={2} sx={{ mt: 1 }}>
          {sale.lines.filter((line) => remaining(line) > 0).map((line) => (
            <Box key={line.id} sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
              <Box sx={{ flex: 1, minWidth: 0 }}>
                <Typography variant="body2" noWrap sx={{ fontWeight: 600 }}>{line.description}</Typography>
                <Typography variant="caption" color="text.secondary">{`${remaining(line)} / ${line.quantity} ${line.unit}`}</Typography>
              </Box>
              <AmountField
                size="small"
                label={t('common:sales.detail.returnQty')}
                value={quantities[line.id] ?? 0}
                error={(quantities[line.id] ?? 0) > remaining(line)}
                onChange={(quantity) => setQuantities({ ...quantities, [line.id]: quantity })}
                sx={{ width: 96 }}
              />
            </Box>
          ))}
          <TextField select size="small" label={t('common:sales.detail.refundMethod')} value={method} onChange={(event) => setMethod(event.target.value as PaymentMethod)}>
            <MenuItem value="Cash">{t('common:sales.methods.Cash')}</MenuItem>
            <MenuItem value="Card">{t('common:sales.methods.Card')}</MenuItem>
            <MenuItem value="BankTransfer">{t('common:sales.methods.BankTransfer')}</MenuItem>
          </TextField>
          <TextField required size="small" label={t('common:sales.detail.reason')} value={reason} onChange={(event) => setReason(event.target.value)} />
          <Typography variant="h6">{`${t('common:sales.detail.refundTotal')}: ${formatMoney(fromCents(refund), lang)}`}</Typography>
          {error && <Alert severity="error">{error}</Alert>}
        </Stack>
      </DialogContent>
      <DialogActions>
        <Button onClick={onClose} disabled={busy}>{t('common:actions.cancel')}</Button>
        <Button variant="contained" disabled={busy || chosen.length === 0 || invalid || reason.trim() === ''} onClick={() => void submit()}>
          {busy ? <CircularProgress size={22} /> : t('common:sales.detail.return')}
        </Button>
      </DialogActions>
    </Dialog>
  )
}

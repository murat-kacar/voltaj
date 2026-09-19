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
  Stack,
  TextField,
  ToggleButton,
  ToggleButtonGroup,
  Typography,
} from '@mui/material'
import SearchIcon from '@mui/icons-material/Search'
import PrintIcon from '@mui/icons-material/Print'
import type { GridColDef } from '@mui/x-data-grid'
import { quickSalesApi, type PaymentMethod, type QuickSale, type QuickSaleSummary, type SaleLine } from './api'
import { AmountField } from './common/AmountField'
import { errorText } from './common/errors'
import { formatMoney } from './common/format'
import { FormDialog } from './common/FormDialog'
import { PagedGrid } from './common/PagedGrid'
import { usePagedQuery } from './common/usePagedQuery'
import { formatDate } from './i18n/formatters'
import { useI18n } from './i18n'
import { rangeStart, saleStatusKey, statusColor } from './quickSaleUtils'
import { Receipt } from './Receipt'
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
  const [selectedId, setSelectedId] = useState<string | null>(null)

  const query = usePagedQuery<QuickSaleSummary>(
    (limit, offset) => quickSalesApi.list({ search: search.trim() || undefined, status: status || undefined, from: rangeStart(range), limit, offset }),
    `${range}|${status}|${search}`,
  )

  const columns = useMemo<GridColDef<QuickSaleSummary>[]>(
    () => [
      { field: 'saleNumber', headerName: t('common:sales.history.columns.number'), width: 130 },
      {
        field: 'soldAt',
        headerName: t('common:sales.history.columns.date'),
        width: 170,
        renderCell: (params) => formatDate(params.row.soldAt, lang),
      },
      { field: 'cashierName', headerName: t('common:sales.history.columns.cashier'), width: 150 },
      {
        field: 'paymentMethods',
        headerName: t('common:sales.history.columns.payment'),
        flex: 1,
        minWidth: 140,
        renderCell: (params) => params.row.paymentMethods.map((method) => t(`common:sales.methods.${method}`)).join(' + '),
      },
      {
        field: 'grandTotal',
        headerName: t('common:sales.history.columns.total'),
        width: 130,
        align: 'right',
        headerAlign: 'right',
        renderCell: (params) => formatMoney(params.row.grandTotal, lang),
      },
      {
        field: 'status',
        headerName: t('common:sales.history.columns.status'),
        width: 130,
        renderCell: (params) => {
          const key = saleStatusKey(params.row.status, params.row.hasReturns)
          return <Chip size="small" color={statusColor(key)} label={t(`common:sales.status.${key}`)} />
        },
      },
    ],
    [t, lang],
  )

  return (
    <Box>
      <Stack direction={{ xs: 'column', md: 'row' }} spacing={2} sx={{ mb: 2 }}>
        <ToggleButtonGroup size="small" exclusive value={range} onChange={(_, value: Range | null) => value && setRange(value)}>
          <ToggleButton value="today">{t('common:sales.history.range.today')}</ToggleButton>
          <ToggleButton value="week">{t('common:sales.history.range.week')}</ToggleButton>
          <ToggleButton value="all">{t('common:sales.history.range.all')}</ToggleButton>
        </ToggleButtonGroup>
        <TextField
          size="small"
          select
          value={status}
          onChange={(event) => setStatus(event.target.value)}
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
          onChange={(event) => setSearch(event.target.value)}
          slotProps={{ input: { startAdornment: <InputAdornment position="start"><SearchIcon /></InputAdornment> } }}
        />
      </Stack>

      <PagedGrid columns={columns} query={query} emptyText={t('common:sales.history.empty')} onRowClick={(row) => setSelectedId(row.id)} />

      {selectedId && (
        <SaleDetailDialog
          id={selectedId}
          isManager={isManager}
          onClose={() => setSelectedId(null)}
          onChanged={() => {
            query.reload()
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

  return (
    <FormDialog
      title={`${t('common:sales.detail.void')} · ${sale.saleNumber}`}
      submitLabel={t('common:sales.detail.void')}
      color="error"
      canSubmit={reason.trim() !== ''}
      onClose={onClose}
      onSubmit={async () => onDone(await quickSalesApi.void(sale.id, reason.trim()))}
    >
      <Typography variant="body2">{t('common:sales.detail.voidPrompt')}</Typography>
      <TextField autoFocus required label={t('common:sales.detail.reason')} value={reason} onChange={(event) => setReason(event.target.value)} />
    </FormDialog>
  )
}

function ReturnDialog({ sale, onClose, onDone }: { sale: QuickSale; onClose: () => void; onDone: (sale: QuickSale) => void }) {
  const { translate: t, lang } = useI18n()
  const [quantities, setQuantities] = useState<Record<string, number>>({})
  const [method, setMethod] = useState<PaymentMethod>('Cash')
  const [reason, setReason] = useState('')

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

  return (
    <FormDialog
      title={`${t('common:sales.detail.return')} · ${sale.saleNumber}`}
      submitLabel={t('common:sales.detail.return')}
      canSubmit={chosen.length > 0 && !invalid && reason.trim() !== ''}
      onClose={onClose}
      onSubmit={async () =>
        onDone(
          await quickSalesApi.return(sale.id, {
            reason: reason.trim(),
            refundMethod: method,
            items: chosen.map((line) => ({ lineId: line.id, quantity: quantities[line.id] })),
          }),
        )
      }
    >
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
    </FormDialog>
  )
}

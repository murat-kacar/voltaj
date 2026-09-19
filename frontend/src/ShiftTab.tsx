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
  Paper,
  Stack,
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableRow,
  TextField,
  Typography,
} from '@mui/material'
import { DataGrid, type GridColDef, type GridPaginationModel } from '@mui/x-data-grid'
import { cashShiftsApi, type CashShift, type ShiftReport } from './api'
import { AmountField } from './AmountField'
import { formatDate } from './i18n/formatters'
import { useI18n } from './i18n'
import { errorText, formatMoney, gridLocaleText } from './quickSaleUtils'

type Props = {
  report: ShiftReport | null
  /** The signed-in user's open shift after a change: the new report when one was opened, null when it was closed. */
  onChanged: (report: ShiftReport | null) => void
}

/** Opens and closes the cashier's shift and shows what it has rung up; managers also see every shift there has been. */
export function ShiftTab({ report, onChanged }: Props) {
  const { translate: t, lang } = useI18n()
  const [openDialog, setOpenDialog] = useState(false)
  const [closeDialog, setCloseDialog] = useState(false)
  const [result, setResult] = useState<ShiftReport | null>(null)

  return (
    <Stack spacing={3}>
      {result && (
        <Alert severity={result.shift.cashDifference === 0 ? 'success' : 'warning'} onClose={() => setResult(null)}>
          {t('common:sales.shift.closedResult', { amount: formatMoney(result.shift.cashDifference ?? 0, lang) })}
        </Alert>
      )}

      {report === null ? (
        <Paper sx={{ p: 4, textAlign: 'center' }}>
          <Typography variant="h6" gutterBottom>{t('common:sales.shift.none')}</Typography>
          <Typography color="text.secondary" sx={{ mb: 2 }}>{t('common:sales.shift.noneHint')}</Typography>
          <Button variant="contained" onClick={() => setOpenDialog(true)}>{t('common:sales.shift.open')}</Button>
        </Paper>
      ) : (
        <Paper sx={{ p: 2 }}>
          <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 2, flexWrap: 'wrap', gap: 1 }}>
            <Box>
              <Typography variant="h6">{t('common:sales.shift.current')}</Typography>
              <Typography variant="body2" color="text.secondary">
                {`${report.shift.cashierName} · ${formatDate(report.shift.openedAt, lang)}`}
              </Typography>
            </Box>
            <Button variant="contained" color="warning" onClick={() => setCloseDialog(true)}>{t('common:sales.shift.close')}</Button>
          </Box>
          <ShiftReportView report={report} />
        </Paper>
      )}

      <ShiftHistory key={report?.shift.id ?? 'none'} />

      {openDialog && (
        <OpenShiftDialog
          onClose={() => setOpenDialog(false)}
          onOpened={(opened) => {
            setOpenDialog(false)
            onChanged(opened)
          }}
        />
      )}
      {closeDialog && report && (
        <CloseShiftDialog
          shiftId={report.shift.id}
          onClose={() => setCloseDialog(false)}
          onClosed={(closed) => {
            setCloseDialog(false)
            setResult(closed)
            onChanged(null)
          }}
        />
      )}
    </Stack>
  )
}

/** The figures of a shift as a plain table, the same for the running shift and for one that is closed. */
export function ShiftReportView({ report }: { report: ShiftReport }) {
  const { translate: t, lang } = useI18n()
  const money = (value: number) => formatMoney(value, lang)
  const { shift } = report
  const rows: { label: string; value: string; strong?: boolean }[] = [
    { label: t('common:sales.shift.opening'), value: money(shift.openingCash) },
    { label: t('common:sales.shift.saleCount'), value: String(report.saleCount) },
    { label: t('common:sales.shift.sales'), value: money(report.salesTotal), strong: true },
    { label: t('common:sales.shift.voidedCount'), value: String(report.voidedCount) },
    { label: t('common:sales.shift.discounts'), value: money(report.discountTotal) },
    { label: t('common:sales.shift.vat'), value: money(report.vatTotal) },
    { label: t('common:sales.shift.cash'), value: money(report.cashSales) },
    { label: t('common:sales.shift.card'), value: money(report.cardSales) },
    { label: t('common:sales.shift.transfer'), value: money(report.transferSales) },
    { label: t('common:sales.shift.returns'), value: `${report.returnCount} · ${money(report.returnTotal)}` },
    { label: t('common:sales.shift.refunds'), value: money(report.cashRefunds) },
    { label: t('common:sales.shift.net'), value: money(report.netSales), strong: true },
    { label: t('common:sales.shift.expected'), value: money(report.expectedCash), strong: true },
  ]
  if (shift.status === 'Closed') {
    rows.push({ label: t('common:sales.shift.counted'), value: money(shift.countedCash ?? 0) })
    rows.push({ label: t('common:sales.shift.difference'), value: money(shift.cashDifference ?? 0), strong: true })
  }

  return (
    <Box>
      <Table size="small">
        <TableBody>
          {rows.map((row) => (
            <TableRow key={row.label}>
              <TableCell sx={{ fontWeight: row.strong ? 700 : 400 }}>{row.label}</TableCell>
              <TableCell align="right" sx={{ fontWeight: row.strong ? 700 : 400 }}>{row.value}</TableCell>
            </TableRow>
          ))}
        </TableBody>
      </Table>

      {report.vatBreakdown.length > 0 && (
        <>
          <Typography variant="subtitle2" sx={{ mt: 2 }}>{t('common:sales.shift.vatBreakdown')}</Typography>
          <Table size="small">
            <TableHead>
              <TableRow>
                <TableCell>{t('common:sales.shift.vatRate')}</TableCell>
                <TableCell align="right">{t('common:sales.shift.gross')}</TableCell>
                <TableCell align="right">{t('common:sales.receipt.vat')}</TableCell>
              </TableRow>
            </TableHead>
            <TableBody>
              {report.vatBreakdown.map((bucket) => (
                <TableRow key={bucket.rate}>
                  <TableCell>{`%${bucket.rate}`}</TableCell>
                  <TableCell align="right">{money(bucket.gross)}</TableCell>
                  <TableCell align="right">{money(bucket.vat)}</TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        </>
      )}
    </Box>
  )
}

function ShiftHistory() {
  const { translate: t, lang } = useI18n()
  const [paging, setPaging] = useState<GridPaginationModel>({ page: 0, pageSize: 10 })
  const [rows, setRows] = useState<CashShift[]>([])
  const [total, setTotal] = useState(0)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')
  const [selected, setSelected] = useState<string | null>(null)

  useEffect(() => {
    let ignore = false
    cashShiftsApi
      .list({ limit: paging.pageSize, offset: paging.page * paging.pageSize })
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
    return () => {
      ignore = true
    }
  }, [paging])

  const columns = useMemo<GridColDef<CashShift>[]>(() => {
    const base = { sortable: false, filterable: false }
    const money = (value?: number | null) => (value === null || value === undefined ? '' : formatMoney(value, lang))
    return [
      { ...base, field: 'openedAt', headerName: t('common:sales.shift.openedAt'), width: 170, renderCell: (params) => formatDate(params.row.openedAt, lang) },
      { ...base, field: 'cashierName', headerName: t('common:sales.shift.cashier'), flex: 1, minWidth: 140 },
      { ...base, field: 'openingCash', headerName: t('common:sales.shift.opening'), width: 130, align: 'right', headerAlign: 'right', renderCell: (params) => money(params.row.openingCash) },
      { ...base, field: 'expectedCash', headerName: t('common:sales.shift.expected'), width: 130, align: 'right', headerAlign: 'right', renderCell: (params) => money(params.row.expectedCash) },
      { ...base, field: 'countedCash', headerName: t('common:sales.shift.counted'), width: 130, align: 'right', headerAlign: 'right', renderCell: (params) => money(params.row.countedCash) },
      {
        ...base,
        field: 'cashDifference',
        headerName: t('common:sales.shift.difference'),
        width: 120,
        align: 'right',
        headerAlign: 'right',
        renderCell: (params) => money(params.row.cashDifference),
      },
      {
        ...base,
        field: 'status',
        headerName: t('common:fields.status'),
        width: 110,
        renderCell: (params) => <Chip size="small" color={params.row.status === 'Open' ? 'success' : 'default'} label={t(`common:sales.shiftStatus.${params.row.status}`)} />,
      },
    ]
  }, [t, lang])

  return (
    <Box>
      <Typography variant="h6" sx={{ mb: 1 }}>{t('common:sales.shift.history')}</Typography>
      {error && <Alert severity="error" sx={{ mb: 2 }}>{error}</Alert>}
      <Paper sx={{ width: '100%' }}>
        <DataGrid
          rows={rows}
          columns={columns}
          loading={loading}
          paginationMode="server"
          rowCount={total}
          paginationModel={paging}
          onPaginationModelChange={(model) => {
            setLoading(true)
            setPaging(model)
          }}
          pageSizeOptions={[10, 25, 50]}
          onRowClick={(params) => setSelected(params.row.id)}
          disableRowSelectionOnClick
          disableColumnFilter
          disableColumnMenu
          autoHeight
          localeText={gridLocaleText(lang)}
          sx={{ '& .MuiDataGrid-row': { cursor: 'pointer' } }}
        />
      </Paper>
      {selected && <ShiftReportDialog id={selected} onClose={() => setSelected(null)} />}
    </Box>
  )
}

function ShiftReportDialog({ id, onClose }: { id: string; onClose: () => void }) {
  const { translate: t, lang } = useI18n()
  const [report, setReport] = useState<ShiftReport | null>(null)
  const [error, setError] = useState('')

  useEffect(() => {
    let ignore = false
    cashShiftsApi
      .report(id)
      .then((loaded) => {
        if (!ignore) setReport(loaded)
      })
      .catch((reason: unknown) => {
        if (!ignore) setError(errorText(reason))
      })
    return () => {
      ignore = true
    }
  }, [id])

  return (
    <Dialog open onClose={onClose} maxWidth="sm" fullWidth>
      <DialogTitle>
        {report ? `${t('common:sales.shift.report')} · ${report.shift.cashierName} · ${formatDate(report.shift.openedAt, lang)}` : t('common:sales.shift.report')}
      </DialogTitle>
      <DialogContent>
        {!report && !error && <Box sx={{ display: 'flex', justifyContent: 'center', p: 3 }}><CircularProgress /></Box>}
        {error && <Alert severity="error">{error}</Alert>}
        {report && <ShiftReportView report={report} />}
      </DialogContent>
      <DialogActions>
        <Button variant="contained" onClick={onClose}>{t('common:actions.close')}</Button>
      </DialogActions>
    </Dialog>
  )
}

function OpenShiftDialog({ onClose, onOpened }: { onClose: () => void; onOpened: (report: ShiftReport) => void }) {
  const { translate: t } = useI18n()
  const [opening, setOpening] = useState(0)
  const [busy, setBusy] = useState(false)
  const [error, setError] = useState('')

  const submit = async () => {
    setBusy(true)
    setError('')
    try {
      onOpened(await cashShiftsApi.open(opening))
    } catch (failure) {
      setError(errorText(failure))
      setBusy(false)
    }
  }

  return (
    <Dialog open onClose={onClose} maxWidth="xs" fullWidth>
      <DialogTitle>{t('common:sales.shift.open')}</DialogTitle>
      <DialogContent>
        <Stack spacing={2} sx={{ mt: 1 }}>
          <AmountField autoFocus label={t('common:sales.shift.opening')} value={opening} onChange={setOpening} />
          {error && <Alert severity="error">{error}</Alert>}
        </Stack>
      </DialogContent>
      <DialogActions>
        <Button onClick={onClose} disabled={busy}>{t('common:actions.cancel')}</Button>
        <Button variant="contained" disabled={busy || opening < 0} onClick={() => void submit()}>
          {busy ? <CircularProgress size={22} /> : t('common:sales.shift.open')}
        </Button>
      </DialogActions>
    </Dialog>
  )
}

function CloseShiftDialog({ shiftId, onClose, onClosed }: { shiftId: string; onClose: () => void; onClosed: (report: ShiftReport) => void }) {
  const { translate: t } = useI18n()
  const [counted, setCounted] = useState(0)
  const [note, setNote] = useState('')
  const [busy, setBusy] = useState(false)
  const [error, setError] = useState('')

  const submit = async () => {
    setBusy(true)
    setError('')
    try {
      onClosed(await cashShiftsApi.close(shiftId, counted, note.trim() || undefined))
    } catch (failure) {
      setError(errorText(failure))
      setBusy(false)
    }
  }

  return (
    <Dialog open onClose={onClose} maxWidth="xs" fullWidth>
      <DialogTitle>{t('common:sales.shift.closeTitle')}</DialogTitle>
      <DialogContent>
        <Stack spacing={2} sx={{ mt: 1 }}>
          <AmountField autoFocus label={t('common:sales.shift.counted')} value={counted} onChange={setCounted} />
          <TextField label={t('common:sales.shift.note')} value={note} onChange={(event) => setNote(event.target.value)} />
          {error && <Alert severity="error">{error}</Alert>}
        </Stack>
      </DialogContent>
      <DialogActions>
        <Button onClick={onClose} disabled={busy}>{t('common:actions.cancel')}</Button>
        <Button variant="contained" color="warning" disabled={busy || counted < 0} onClick={() => void submit()}>
          {busy ? <CircularProgress size={22} /> : t('common:sales.shift.close')}
        </Button>
      </DialogActions>
    </Dialog>
  )
}

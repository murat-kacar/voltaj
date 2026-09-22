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
import type { GridColDef } from '@mui/x-data-grid'
import { cashShiftsApi, type CashShift, type ShiftReport } from './api'
import { AmountField } from './common/AmountField'
import { errorText } from './common/errors'
import { formatMoney } from './common/format'
import { FormDialog } from './common/FormDialog'
import { PagedGrid } from './common/PagedGrid'
import { usePagedQuery } from './common/usePagedQuery'
import { formatDate } from './i18n/formatters'
import { useI18n } from './i18n'

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
          <Button variant="contained" onClick={() => setOpenDialog(true)} data-testid="button-af8b26">{t('common:sales.shift.open')}</Button>
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
            <Button variant="contained" color="warning" onClick={() => setCloseDialog(true)} data-testid="button-e06371">{t('common:sales.shift.close')}</Button>
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
  const [selected, setSelected] = useState<string | null>(null)
  const query = usePagedQuery<CashShift>((limit, offset) => cashShiftsApi.list({ limit, offset }), '', 10)

  const columns = useMemo<GridColDef<CashShift>[]>(() => {
    const money = (value?: number | null) => (value === null || value === undefined ? '' : formatMoney(value, lang))
    return [
      { field: 'openedAt', headerName: t('common:sales.shift.openedAt'), width: 170, renderCell: (params) => formatDate(params.row.openedAt, lang) },
      { field: 'cashierName', headerName: t('common:sales.shift.cashier'), flex: 1, minWidth: 140 },
      { field: 'openingCash', headerName: t('common:sales.shift.opening'), width: 130, align: 'right', headerAlign: 'right', renderCell: (params) => money(params.row.openingCash) },
      { field: 'expectedCash', headerName: t('common:sales.shift.expected'), width: 130, align: 'right', headerAlign: 'right', renderCell: (params) => money(params.row.expectedCash) },
      { field: 'countedCash', headerName: t('common:sales.shift.counted'), width: 130, align: 'right', headerAlign: 'right', renderCell: (params) => money(params.row.countedCash) },
      { field: 'cashDifference', headerName: t('common:sales.shift.difference'), width: 120, align: 'right', headerAlign: 'right', renderCell: (params) => money(params.row.cashDifference) },
      {
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
      <PagedGrid columns={columns} query={query} pageSizeOptions={[10, 25, 50]} onRowClick={(row) => setSelected(row.id)} />
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
    <Dialog open onClose={onClose} maxWidth="sm" fullWidth data-testid="dialog-1c3c67">
      <DialogTitle>
        {report ? `${t('common:sales.shift.report')} · ${report.shift.cashierName} · ${formatDate(report.shift.openedAt, lang)}` : t('common:sales.shift.report')}
      </DialogTitle>
      <DialogContent>
        {!report && !error && <Box sx={{ display: 'flex', justifyContent: 'center', p: 3 }}><CircularProgress /></Box>}
        {error && <Alert severity="error">{error}</Alert>}
        {report && <ShiftReportView report={report} />}
      </DialogContent>
      <DialogActions>
        <Button variant="contained" onClick={onClose} data-testid="button-af9677">{t('common:actions.close')}</Button>
      </DialogActions>
    </Dialog>
  )
}

function OpenShiftDialog({ onClose, onOpened }: { onClose: () => void; onOpened: (report: ShiftReport) => void }) {
  const { translate: t } = useI18n()
  const [opening, setOpening] = useState(0)

  return (
    <FormDialog
      title={t('common:sales.shift.open')}
      submitLabel={t('common:sales.shift.open')}
      canSubmit={opening >= 0}
      onClose={onClose}
      onSubmit={async () => onOpened(await cashShiftsApi.open(opening))}
    >
      <AmountField autoFocus label={t('common:sales.shift.opening')} value={opening} onChange={setOpening} />
    </FormDialog>
  )
}

function CloseShiftDialog({ shiftId, onClose, onClosed }: { shiftId: string; onClose: () => void; onClosed: (report: ShiftReport) => void }) {
  const { translate: t } = useI18n()
  const [counted, setCounted] = useState(0)
  const [note, setNote] = useState('')

  return (
    <FormDialog
      title={t('common:sales.shift.closeTitle')}
      submitLabel={t('common:sales.shift.close')}
      color="warning"
      canSubmit={counted >= 0}
      onClose={onClose}
      onSubmit={async () => onClosed(await cashShiftsApi.close(shiftId, counted, note.trim() || undefined))}
    >
      <AmountField autoFocus label={t('common:sales.shift.counted')} value={counted} onChange={setCounted} />
      <TextField label={t('common:sales.shift.note')} value={note} onChange={(event) => setNote(event.target.value)}  data-testid="textfield-26e747" />
    </FormDialog>
  )
}

import { useEffect, useState } from 'react'
import { useParams, useNavigate, useLocation } from 'react-router-dom'
import {
  Alert, Box, Button, CircularProgress, Dialog, DialogActions, DialogContent,
  DialogTitle, Divider, MenuItem, Paper, Stack, TextField, Typography,
} from '@mui/material'
import ArrowBackIcon from '@mui/icons-material/ArrowBack'
import { paymentsApi, type PaymentRow, type SalesInvoiceRow } from '../api'
import { formatDay, formatMoney } from '../common/format'
import { useI18n } from '../i18n'

function SummaryRow({ label, value, strong }: { label: string; value: string; strong?: boolean }) {
  return (
    <Box sx={{ display: 'flex', justifyContent: 'space-between', py: 0.75 }}>
      <Typography variant="body2" color={strong ? 'text.primary' : 'text.secondary'}>{label}</Typography>
      <Typography variant="body2" sx={{ fontWeight: strong ? 700 : 400 }}>{value}</Typography>
    </Box>
  )
}

export function InvoiceDetailView() {
  const { id } = useParams<{ id: string }>()
  const navigate = useNavigate()
  const location = useLocation()
  const { translate: t, lang } = useI18n()

  // The invoice data can be passed via router state (from the list) or fetched from the list.
  const stateInvoice = (location.state as { invoice?: SalesInvoiceRow } | null)?.invoice
  const [invoice, setInvoice] = useState<SalesInvoiceRow | null>(stateInvoice ?? null)
  const [loadError, setLoadError] = useState('')

  // Unallocated payments for this customer
  const [payments, setPayments] = useState<PaymentRow[]>([])
  const [loadingPayments, setLoadingPayments] = useState(false)

  // Allocate dialog
  const [showAllocate, setShowAllocate] = useState(false)
  const [selectedPaymentId, setSelectedPaymentId] = useState('')
  const [allocateAmount, setAllocateAmount] = useState('')
  const [allocateBusy, setAllocateBusy] = useState(false)
  const [allocateMsg, setAllocateMsg] = useState<{ type: 'success' | 'error'; text: string } | null>(null)

  // Record new payment dialog
  const [showRecord, setShowRecord] = useState(false)
  const [newAmount, setNewAmount] = useState('')
  const [newMethod, setNewMethod] = useState<'Cash' | 'Card' | 'BankTransfer'>('Cash')
  const [newDate, setNewDate] = useState(() => new Date().toISOString().slice(0, 10))
  const [recordBusy, setRecordBusy] = useState(false)
  const [recordMsg, setRecordMsg] = useState<{ type: 'success' | 'error'; text: string } | null>(null)

  // Fetch invoice from the list if not provided via state
  useEffect(() => {
    if (invoice || !id) return
    paymentsApi.listInvoices({ limit: 200 })
      .then(page => {
        const found = page.items.find(i => i.id === id)
        if (found) setInvoice(found)
        else setLoadError(t('payments:invoice.loadFailed'))
      })
      .catch(() => setLoadError(t('payments:invoice.loadFailed')))
  }, [id, invoice, t])

  // Load unallocated payments when allocate dialog opens
  useEffect(() => {
    if (!showAllocate || !invoice) return
    setLoadingPayments(true)
    paymentsApi.list({ customerId: invoice.customerId, limit: 100 })
      .then(page => { setPayments(page.items); setLoadingPayments(false) })
      .catch(() => setLoadingPayments(false))
  }, [showAllocate, invoice])

  const handleAllocate = async () => {
    if (!invoice || !selectedPaymentId || !allocateAmount) return
    setAllocateBusy(true)
    setAllocateMsg(null)
    try {
      await paymentsApi.allocate({ paymentId: selectedPaymentId, invoiceId: invoice.id, amount: parseFloat(allocateAmount) })
      setAllocateMsg({ type: 'success', text: t('payments:invoice.allocateSuccess') })
      const updated = await paymentsApi.listInvoices({ limit: 200 }).then(p => p.items.find(i => i.id === invoice.id))
      if (updated) setInvoice(updated)
      setShowAllocate(false)
      setSelectedPaymentId('')
      setAllocateAmount('')
    } catch {
      setAllocateMsg({ type: 'error', text: t('payments:invoice.allocateFailed') })
    } finally {
      setAllocateBusy(false)
    }
  }

  const handleRecord = async () => {
    if (!invoice || !newAmount) return
    setRecordBusy(true)
    setRecordMsg(null)
    try {
      const payment = await paymentsApi.create({
        customerId: invoice.customerId,
        amount: parseFloat(newAmount),
        paymentMethod: newMethod,
        paymentDate: newDate,
      })
      await paymentsApi.allocate({
        paymentId: payment.id,
        invoiceId: invoice.id,
        amount: parseFloat(newAmount),
      })
      setRecordMsg({ type: 'success', text: t('payments:invoice.recordSuccess') })
      const updated = await paymentsApi.listInvoices({ limit: 200 }).then(p => p.items.find(i => i.id === invoice.id))
      if (updated) setInvoice(updated)
      setShowRecord(false)
      setNewAmount('')
      setNewMethod('Cash')
      setNewDate(new Date().toISOString().slice(0, 10))
    } catch {
      setRecordMsg({ type: 'error', text: t('payments:invoice.recordFailed') })
    } finally {
      setRecordBusy(false)
    }
  }

  if (!invoice && !loadError) return <Box sx={{ display: 'flex', justifyContent: 'center', p: 6 }}><CircularProgress /></Box>
  if (loadError || !invoice) return (
    <Box sx={{ p: 3 }}>
      <Alert severity="error">{loadError}</Alert>
      <Button sx={{ mt: 2 }} startIcon={<ArrowBackIcon />} onClick={() => navigate('/invoices')} data-testid="button-6a1cf8">{t('payments:invoice.back')}</Button>
    </Box>
  )

  const isFullyPaid = invoice.remainingAmount <= 0
  const selectedPayment = payments.find(p => p.id === selectedPaymentId)

  return (
    <Box sx={{ maxWidth: 640 }}>
      <Button size="small" startIcon={<ArrowBackIcon />} onClick={() => navigate('/invoices')} sx={{ mb: 2 }} data-testid="button-49ae34">
        {t('payments:invoice.back')}
      </Button>

      <Box sx={{ mb: 3 }}>
        <Typography variant="overline" color="text.secondary">{t('payments:invoice.title')}</Typography>
        <Typography variant="h4">{invoice.invoiceNumber}</Typography>
        <Typography variant="body2" color="text.secondary">{invoice.customerName} · {formatDay(invoice.invoiceDate, lang)}</Typography>
      </Box>

      {(allocateMsg || recordMsg) && (
        <Alert severity={(allocateMsg ?? recordMsg)!.type} sx={{ mb: 2 }} onClose={() => { setAllocateMsg(null); setRecordMsg(null) }}>
          {(allocateMsg ?? recordMsg)!.text}
        </Alert>
      )}

      <Paper sx={{ p: 3, mb: 3 }}>
        <Typography variant="subtitle2" sx={{ fontWeight: 600 }} gutterBottom>{t('payments:invoice.summary')}</Typography>
        <Divider sx={{ mb: 1 }} />
        <SummaryRow label={t('payments:invoice.grandTotal')} value={formatMoney(invoice.grandTotal, lang)} />
        <SummaryRow label={t('payments:invoice.paid')} value={formatMoney(invoice.paidAmount + invoice.appliedDepositAmount, lang)} />
        <Divider sx={{ my: 0.5 }} />
        <SummaryRow
          label={isFullyPaid ? t('payments:invoice.fullyPaid') : t('payments:invoice.remaining')}
          value={formatMoney(invoice.remainingAmount, lang)}
          strong
        />
      </Paper>

      {!isFullyPaid && (
        <Stack direction={{ xs: 'column', sm: 'row' }} spacing={2}>
          <Button variant="contained" onClick={() => setShowRecord(true)} data-testid="button-e642f8">
            {t('payments:invoice.recordNew')}
          </Button>
          <Button variant="outlined" onClick={() => setShowAllocate(true)} data-testid="button-348339">
            {t('payments:invoice.allocate')}
          </Button>
        </Stack>
      )}

      {/* Record new payment dialog */}
      <Dialog open={showRecord} onClose={() => setShowRecord(false)} maxWidth="xs" fullWidth data-testid="dialog-c8a668">
        <DialogTitle>{t('payments:invoice.recordTitle')}</DialogTitle>
        <DialogContent>
          <Stack spacing={2} sx={{ mt: 1 }}>
            {recordMsg && <Alert severity={recordMsg.type}>{recordMsg.text}</Alert>}
            <TextField
              label={t('payments:form.amount')}
              type="number"
              value={newAmount}
              onChange={e => setNewAmount(e.target.value)}
              slotProps={{ htmlInput: { min: 0.01, step: 0.01 } }}
              fullWidth
             data-testid="textfield-8e6085" />
            <TextField select label={t('payments:invoice.method')} value={newMethod} onChange={e => setNewMethod(e.target.value as typeof newMethod)} fullWidth data-testid="textfield-190f9d">
              {(['Cash', 'Card', 'BankTransfer'] as const).map(m => (
                <MenuItem key={m} value={m} data-testid="menuitem-5c42a1">{t(`payments:invoice.methods.${m}`)}</MenuItem>
              ))}
            </TextField>
            <TextField
              label={t('payments:invoice.paymentDate')}
              type="date"
              value={newDate}
              onChange={e => setNewDate(e.target.value)}
              fullWidth
             data-testid="textfield-c68760" />
          </Stack>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setShowRecord(false)} data-testid="button-e57fb2">{t('common:actions.cancel')}</Button>
          <Button
            variant="contained"
            disabled={recordBusy || !newAmount || parseFloat(newAmount) <= 0}
            onClick={handleRecord}
           data-testid="button-cb320f">
            {recordBusy ? <CircularProgress size={20} /> : t('payments:invoice.recordSubmit')}
          </Button>
        </DialogActions>
      </Dialog>

      {/* Allocate existing payment dialog */}
      <Dialog open={showAllocate} onClose={() => setShowAllocate(false)} maxWidth="xs" fullWidth data-testid="dialog-b709c1">
        <DialogTitle>{t('payments:invoice.allocateTitle')}</DialogTitle>
        <DialogContent>
          <Stack spacing={2} sx={{ mt: 1 }}>
            {allocateMsg && <Alert severity={allocateMsg.type}>{allocateMsg.text}</Alert>}
            {loadingPayments && <CircularProgress size={24} />}
            {!loadingPayments && payments.length === 0 && (
              <Typography color="text.secondary">{t('payments:invoice.noPayments')}</Typography>
            )}
            {!loadingPayments && payments.length > 0 && (
              <>
                <TextField select label={t('payments:invoice.selectPayment')} value={selectedPaymentId} onChange={e => {
                  setSelectedPaymentId(e.target.value)
                  const p = payments.find(x => x.id === e.target.value)
                  if (p) setAllocateAmount(String(Math.min(p.amount, invoice.remainingAmount)))
                }} fullWidth data-testid="textfield-514fd2">
                  {payments.map(p => (
                    <MenuItem key={p.id} value={p.id} data-testid="menuitem-ef1a1b">
                      {formatMoney(p.amount, lang)} · {formatDay(p.paymentDate, lang)} · {p.paymentMethod}
                    </MenuItem>
                  ))}
                </TextField>
                <TextField
                  label={t('payments:invoice.amount')}
                  type="number"
                  value={allocateAmount}
                  onChange={e => setAllocateAmount(e.target.value)}
                  slotProps={{ htmlInput: { min: 0.01, step: 0.01, max: selectedPayment?.amount ?? invoice.remainingAmount } }}
                  fullWidth
                 data-testid="textfield-0d2ddc" />
              </>
            )}
          </Stack>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setShowAllocate(false)} data-testid="button-b9bff4">{t('common:actions.cancel')}</Button>
          <Button
            variant="contained"
            disabled={allocateBusy || !selectedPaymentId || !allocateAmount || parseFloat(allocateAmount) <= 0}
            onClick={handleAllocate}
           data-testid="button-ab8c7f">
            {allocateBusy ? <CircularProgress size={20} /> : t('payments:invoice.allocateSubmit')}
          </Button>
        </DialogActions>
      </Dialog>
    </Box>
  )
}

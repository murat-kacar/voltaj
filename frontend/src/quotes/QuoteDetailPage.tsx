import { useEffect, useState, type ReactNode } from 'react'
import { useNavigate, useParams } from 'react-router-dom'
import {
  Alert, Box, Button, Chip, CircularProgress, Divider, InputAdornment,
  Stack, Table, TableBody, TableCell, TableContainer, TableHead, TableRow,
  TextField, Typography, useMediaQuery,
} from '@mui/material'
import ArrowBackIcon from '@mui/icons-material/ArrowBack'
import { useTheme } from '@mui/material/styles'
import { quotesApi, sessionRoles, type Quote } from '../api'
import { AmountField } from '../common/AmountField'
import { errorText } from '../common/errors'
import { formatDay, formatMoney } from '../common/format'
import { FormDialog } from '../common/FormDialog'
import { useI18n } from '../i18n'
import { formatDate } from '../i18n/formatters'
import { divRound, fromCents, toCents } from '../saleMath'
import { QuoteFormDialog } from './QuoteFormDialog'
import { QuoteStateChip } from './QuoteStateChip'
import { QuoteTotalsBlock } from './QuoteTotalsBlock'
import { isLapsed, totalsOfLines } from './quoteMath'

type Action = 'edit' | 'delete' | 'issue' | 'accept' | 'reject' | 'withdraw' | 'deposit' | 'convert' | 'copy'
const dayOptions = { year: 'numeric', month: 'short', day: 'numeric' } as const

function Info({ label, children }: { label: string; children: ReactNode }) {
  return (
    <>
      <Typography color="text.secondary">{label}</Typography>
      <Typography component="div">{children}</Typography>
    </>
  )
}

type ConfirmProps = {
  title: string
  body: string
  submitLabel: string
  color?: 'primary' | 'warning'
  onClose: () => void
  onConfirm: () => Promise<void>
}

function ConfirmDialog({ title, body, submitLabel, color, onClose, onConfirm }: ConfirmProps) {
  return (
    <FormDialog title={title} submitLabel={submitLabel} color={color} onClose={onClose} onSubmit={onConfirm}>
      <Typography>{body}</Typography>
    </FormDialog>
  )
}

type StepProps = { quote: Quote; onClose: () => void; onDone: (quote: Quote) => void }

function AcceptDialog({ quote, onClose, onDone }: StepProps) {
  const { translate: t, lang } = useI18n()
  const [percentage, setPercentage] = useState(quote.requiredDepositPercentage)
  const invalid = percentage < 0 || percentage > 100
  const amount = fromCents(divRound(toCents(quote.total) * Math.round(percentage * 100), 10000))
  return (
    <FormDialog title={t('quotes:dialogs.accept.title')} submitLabel={t('quotes:actions.accept')} canSubmit={!invalid} onClose={onClose} onSubmit={async () => onDone(await quotesApi.accept(quote.id, percentage))}>
      <Typography>{t('quotes:dialogs.accept.body')}</Typography>
      <AmountField autoFocus label={t('quotes:dialogs.accept.percentage')} value={percentage} error={invalid} onChange={setPercentage} slotProps={{ input: { endAdornment: <InputAdornment position="end">{'%'}</InputAdornment> } }} />
      {!invalid && percentage > 0 && <Typography variant="body2" color="text.secondary">{t('quotes:dialogs.accept.amount', { amount: formatMoney(amount, lang) })}</Typography>}
    </FormDialog>
  )
}

function RejectDialog({ quote, onClose, onDone }: StepProps) {
  const { translate: t } = useI18n()
  const [reason, setReason] = useState('')
  return (
    <FormDialog title={t('quotes:dialogs.reject.title')} submitLabel={t('quotes:actions.reject')} color="warning" canSubmit={reason.trim() !== ''} onClose={onClose} onSubmit={async () => onDone(await quotesApi.reject(quote.id, reason.trim()))}>
      <TextField autoFocus multiline minRows={2} label={t('quotes:dialogs.reject.reason')} value={reason} onChange={(e) => setReason(e.target.value)}  data-testid="textfield-c13e7b" />
    </FormDialog>
  )
}

function DepositDialog({ quote, onClose, onDone }: StepProps) {
  const { translate: t, lang } = useI18n()
  const stillOwed = Math.max(0, quote.requiredDepositAmount - quote.depositPaidAmount)
  const room = fromCents(toCents(quote.total) - toCents(quote.depositPaidAmount))
  const [amount, setAmount] = useState(stillOwed)
  const invalid = amount <= 0 || toCents(amount) > toCents(room)
  return (
    <FormDialog title={t('quotes:dialogs.deposit.title')} submitLabel={t('quotes:actions.deposit')} canSubmit={!invalid} onClose={onClose} onSubmit={async () => onDone(await quotesApi.payDeposit(quote.id, amount))}>
      <Typography variant="body2" color="text.secondary">{t('quotes:dialogs.deposit.hint', { required: formatMoney(quote.requiredDepositAmount, lang), paid: formatMoney(quote.depositPaidAmount, lang), room: formatMoney(room, lang) })}</Typography>
      <AmountField autoFocus label={t('quotes:dialogs.deposit.amount')} value={amount} error={invalid && amount !== 0} onChange={setAmount} />
    </FormDialog>
  )
}

export function QuoteDetailPage() {
  const { id } = useParams<{ id: string }>()
  const navigate = useNavigate()
  const { translate: t, lang } = useI18n()
  const canEdit = sessionRoles().some((role) => role === 'Admin' || role === 'Manager')
  const mobile = useMediaQuery(useTheme().breakpoints.down('sm'))

  const [quote, setQuote] = useState<Quote | null>(null)
  const [loadError, setLoadError] = useState('')
  const [action, setAction] = useState<Action | null>(null)

  useEffect(() => {
    if (!id) return
    let ignore = false
    quotesApi.get(id)
      .then((loaded) => { if (!ignore) setQuote(loaded) })
      .catch((reason: unknown) => { if (!ignore) setLoadError(errorText(reason)) })
    return () => { ignore = true }
  }, [id])

  const changed = (updated: Quote) => { setQuote(updated); setAction(null) }

  if (!quote && !loadError) return <Box sx={{ display: 'flex', justifyContent: 'center', p: 6 }}><CircularProgress /></Box>
  if (loadError || !quote) return (
    <Box sx={{ p: 3 }}>
      <Alert severity="error">{loadError || t('quotes:page.loadFailed')}</Alert>
      <Button sx={{ mt: 2 }} startIcon={<ArrowBackIcon />} onClick={() => navigate('/quotes')} data-testid="button-6a5014">{t('quotes:page.back')}</Button>
    </Box>
  )

  const money = (value: number) => formatMoney(value, lang)
  const lapsed = isLapsed(quote.state, quote.validUntil)
  const totals = totalsOfLines(quote.items)
  const remainingDeposit = Math.max(0, quote.requiredDepositAmount - quote.depositPaidAmount)

  return (
    <Box sx={{ maxWidth: 800 }}>
      <Button size="small" startIcon={<ArrowBackIcon />} onClick={() => navigate('/quotes')} sx={{ mb: 2 }} data-testid="button-e7bbf3">
        {t('quotes:page.back')}
      </Button>

      <Box sx={{ display: 'flex', alignItems: 'center', gap: 1.5, flexWrap: 'wrap', mb: 1 }}>
        <Typography variant="h4">{quote.number}</Typography>
        <QuoteStateChip state={quote.state} validUntil={quote.validUntil} />
      </Box>
      <Typography variant="body2" color="text.secondary" sx={{ mb: 3 }}>{quote.title}</Typography>

      {lapsed && <Alert severity="warning" sx={{ mb: 2 }}>{t('quotes:detail.lapsed')}</Alert>}
      {quote.state === 'Rejected' && quote.rejectionReason && (
        <Alert severity="error" sx={{ mb: 2 }}>{t('quotes:detail.rejectedBecause', { reason: quote.rejectionReason })}</Alert>
      )}
      {quote.workOrderNumber && <Alert severity="success" sx={{ mb: 2 }}>{t('quotes:detail.workOrderMade', { number: quote.workOrderNumber })}</Alert>}

      <Box sx={{ display: 'grid', gridTemplateColumns: { xs: '110px 1fr', sm: '150px 1fr' }, rowGap: 1, columnGap: 2, mb: 3 }}>
        <Info label={t('quotes:detail.customer')}>{[quote.customerName, quote.customerPhone].filter(Boolean).join(' · ')}</Info>
        <Info label={t('quotes:detail.title')}>{quote.title}</Info>
        {quote.siteName && <Info label={t('quotes:detail.address')}>{`${quote.siteName}${quote.siteAddress ? ` — ${quote.siteAddress}` : ''}`}</Info>}
        {quote.assetName && <Info label={t('quotes:detail.device')}>{quote.assetName}</Info>}
        <Info label={t('quotes:detail.created')}>{formatDate(quote.createdAt, lang, dayOptions)}</Info>
        {quote.issuedAt && <Info label={t('quotes:detail.issued')}>{formatDate(quote.issuedAt, lang, dayOptions)}</Info>}
        <Info label={t('quotes:detail.validUntil')}>{quote.validUntil ? formatDay(quote.validUntil, lang) : t('quotes:detail.validUntilOnIssue')}</Info>
        {quote.decidedAt && <Info label={t('quotes:detail.decided')}>{formatDate(quote.decidedAt, lang, dayOptions)}</Info>}
        {quote.notes && <Info label={t('quotes:detail.notes')}><span style={{ whiteSpace: 'pre-line' }}>{quote.notes}</span></Info>}
      </Box>

      {quote.items.length === 0 ? (
        <Typography color="text.secondary" sx={{ mb: 3 }}>{t('quotes:detail.noLines')}</Typography>
      ) : mobile ? (
        <Stack divider={<Divider flexItem />} spacing={1} sx={{ mb: 3 }}>
          {quote.items.map((item) => (
            <Box key={item.id} sx={{ display: 'flex', justifyContent: 'space-between', gap: 2 }}>
              <Box sx={{ minWidth: 0 }}>
                <Typography>{item.description}</Typography>
                <Typography variant="body2" color="text.secondary">
                  {`${t(`quotes:kind.${item.kind}`)} · ${item.quantity} ${item.unit} × ${money(item.unitPrice)} · %${item.vatRate}`}
                </Typography>
              </Box>
              <Typography sx={{ fontWeight: 600, whiteSpace: 'nowrap' }}>{money(item.lineTotal)}</Typography>
            </Box>
          ))}
        </Stack>
      ) : (
        <TableContainer sx={{ mb: 3 }}>
          <Table size="small">
            <TableHead>
              <TableRow>
                <TableCell>{t('quotes:document.lineNumber')}</TableCell>
                <TableCell>{t('quotes:document.description')}</TableCell>
                <TableCell align="right">{t('quotes:document.quantity')}</TableCell>
                <TableCell align="right">{t('quotes:document.unitPrice')}</TableCell>
                <TableCell align="right">{t('quotes:document.vatRate')}</TableCell>
                <TableCell align="right">{t('quotes:document.lineTotal')}</TableCell>
              </TableRow>
            </TableHead>
            <TableBody>
              {quote.items.map((item) => (
                <TableRow key={item.id}>
                  <TableCell>{item.lineNumber}</TableCell>
                  <TableCell>
                    {item.description} <Chip size="small" variant="outlined" label={t(`quotes:kind.${item.kind}`)} sx={{ ml: 0.5 }} />
                  </TableCell>
                  <TableCell align="right">{`${item.quantity} ${item.unit}`}</TableCell>
                  <TableCell align="right">{money(item.unitPrice)}</TableCell>
                  <TableCell align="right">{`%${item.vatRate}`}</TableCell>
                  <TableCell align="right">{money(item.lineTotal)}</TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        </TableContainer>
      )}

      <QuoteTotalsBlock net={totals.net} vat={quote.vatTotal} total={quote.total} />

      {(quote.state === 'Accepted' || quote.requiredDepositPercentage > 0) && (
        <Alert severity="info" icon={false} sx={{ mt: 2 }}>
          {t('quotes:detail.deposit', {
            rate: quote.requiredDepositPercentage,
            required: money(quote.requiredDepositAmount),
            paid: money(quote.depositPaidAmount),
            remaining: money(remainingDeposit),
          })}
        </Alert>
      )}

      {!canEdit && <Typography variant="caption" color="text.secondary" sx={{ display: 'block', mt: 2 }}>{t('quotes:detail.readOnly')}</Typography>}

      <Stack direction="row" spacing={1} sx={{ mt: 3, gap: 1, flexWrap: 'wrap' }}>
        {canEdit && quote.state === 'Draft' && (
          <>
            <Button color="error" onClick={() => setAction('delete')} data-testid="button-33e6a7">{t('quotes:actions.delete')}</Button>
            <Button onClick={() => setAction('edit')} data-testid="button-09f819">{t('quotes:actions.edit')}</Button>
          </>
        )}
        {canEdit && quote.state === 'Issued' && <Button onClick={() => setAction('withdraw')} data-testid="button-caeda1">{t('quotes:actions.withdraw')}</Button>}
        {canEdit && quote.state !== 'Draft' && <Button onClick={() => setAction('copy')} data-testid="button-7e6975">{t('quotes:actions.revise')}</Button>}
        {canEdit && quote.state === 'Issued' && <Button color="error" onClick={() => setAction('reject')} data-testid="button-266bd6">{t('quotes:actions.reject')}</Button>}
        {canEdit && quote.state === 'Draft' && (
          <Button variant="contained" disabled={quote.items.length === 0} onClick={() => setAction('issue')} data-testid="button-18f703">{t('quotes:actions.issue')}</Button>
        )}
        {canEdit && quote.state === 'Issued' && (
          <Button variant="contained" disabled={lapsed} onClick={() => setAction('accept')} data-testid="button-5fd170">{t('quotes:actions.accept')}</Button>
        )}
        {canEdit && quote.state === 'Accepted' && (
          <>
            {remainingDeposit > 0 && <Button onClick={() => setAction('deposit')} data-testid="button-7c8ffe">{t('quotes:actions.deposit')}</Button>}
            {!quote.workOrderId && <Button variant="contained" onClick={() => setAction('convert')} data-testid="button-229474">{t('quotes:actions.convert')}</Button>}
          </>
        )}
      </Stack>

      {quote && action === 'edit' && <QuoteFormDialog quote={quote} onClose={() => setAction(null)} onSaved={changed} />}
      {quote && action === 'issue' && (
        <ConfirmDialog title={t('quotes:dialogs.issue.title')} body={t('quotes:dialogs.issue.body')} submitLabel={t('quotes:actions.issue')} onClose={() => setAction(null)} onConfirm={async () => changed(await quotesApi.issue(quote.id))} />
      )}
      {quote && action === 'accept' && <AcceptDialog quote={quote} onClose={() => setAction(null)} onDone={changed} />}
      {quote && action === 'reject' && <RejectDialog quote={quote} onClose={() => setAction(null)} onDone={changed} />}
      {quote && action === 'withdraw' && (
        <ConfirmDialog title={t('quotes:dialogs.withdraw.title')} body={t('quotes:dialogs.withdraw.body')} submitLabel={t('quotes:actions.withdraw')} color="warning" onClose={() => setAction(null)} onConfirm={async () => changed(await quotesApi.expire(quote.id))} />
      )}
      {quote && action === 'deposit' && <DepositDialog quote={quote} onClose={() => setAction(null)} onDone={changed} />}
      {quote && action === 'convert' && (
        <ConfirmDialog title={t('quotes:dialogs.convert.title')} body={t('quotes:dialogs.convert.body')} submitLabel={t('quotes:actions.convert')} onClose={() => setAction(null)} onConfirm={async () => {
          await quotesApi.convertToWorkOrder(quote.id)
          changed(await quotesApi.get(quote.id))
        }} />
      )}
      {quote && action === 'copy' && (
        <ConfirmDialog title={t('quotes:dialogs.revise.title')} body={t('quotes:dialogs.revise.body')} submitLabel={t('quotes:actions.revise')} onClose={() => setAction(null)} onConfirm={async () => {
          const copy = await quotesApi.copy(quote.id)
          navigate('/quotes/' + copy.id)
        }} />
      )}
      {quote && action === 'delete' && (
        <ConfirmDialog title={t('quotes:dialogs.delete.title')} body={t('quotes:dialogs.delete.body')} submitLabel={t('quotes:actions.delete')} color="warning" onClose={() => setAction(null)} onConfirm={async () => {
          await quotesApi.remove(quote.id)
          navigate('/quotes')
        }} />
      )}
    </Box>
  )
}

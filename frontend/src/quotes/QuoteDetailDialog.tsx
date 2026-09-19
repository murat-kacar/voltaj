import { useEffect, useState, type ReactNode } from 'react'
import { createPortal } from 'react-dom'
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
  Divider,
  InputAdornment,
  Stack,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  TextField,
  Typography,
  useMediaQuery,
} from '@mui/material'
import { useTheme } from '@mui/material/styles'
import PrintIcon from '@mui/icons-material/Print'
import { quotesApi, type Quote } from '../api'
import { AmountField } from '../common/AmountField'
import { errorText } from '../common/errors'
import { formatDay, formatMoney } from '../common/format'
import { FormDialog } from '../common/FormDialog'
import { useI18n } from '../i18n'
import { formatDate } from '../i18n/formatters'
import { divRound, fromCents, toCents } from '../saleMath'
import { QuoteDocument } from './QuoteDocument'
import { QuoteFormDialog } from './QuoteFormDialog'
import { QuoteStateChip } from './QuoteStateChip'
import { QuoteTotalsBlock } from './QuoteTotalsBlock'
import { isLapsed, totalsOfLines } from './quoteMath'

type Action = 'edit' | 'delete' | 'issue' | 'accept' | 'reject' | 'withdraw' | 'deposit' | 'convert' | 'copy'

type Props = {
  id: string
  /** Managers can change a quote; everyone else only looks. */
  canEdit: boolean
  onClose: () => void
  /** The quote was changed, so the list behind is stale. */
  onChanged: () => void
  /** Another quote (the copy that was just made) should be shown in its place. */
  onOpen: (id: string) => void
}

const dayOptions = { year: 'numeric', month: 'short', day: 'numeric' } as const

/** One quote: what it says, where it stands, and what can be done with it next. Also the place it is printed from. */
export function QuoteDetailDialog({ id, canEdit, onClose, onChanged, onOpen }: Props) {
  const { translate: t, lang } = useI18n()
  const fullScreen = useMediaQuery(useTheme().breakpoints.down('sm'))
  const [quote, setQuote] = useState<Quote | null>(null)
  const [error, setError] = useState('')
  const [action, setAction] = useState<Action | null>(null)

  useEffect(() => {
    let ignore = false
    quotesApi
      .get(id)
      .then((loaded) => {
        if (!ignore) setQuote(loaded)
      })
      .catch((reason: unknown) => {
        if (!ignore) setError(errorText(reason))
      })
    return () => {
      ignore = true
    }
  }, [id])

  const changed = (updated: Quote) => {
    setQuote(updated)
    setAction(null)
    onChanged()
  }

  const money = (value: number) => formatMoney(value, lang)
  const lapsed = quote ? isLapsed(quote.state, quote.validUntil) : false
  const totals = quote ? totalsOfLines(quote.items) : null
  const remainingDeposit = quote ? Math.max(0, quote.requiredDepositAmount - quote.depositPaidAmount) : 0

  return (
    <Dialog open onClose={onClose} maxWidth="md" fullWidth fullScreen={fullScreen}>
      <DialogTitle>
        {quote ? (
          <Box sx={{ display: 'flex', alignItems: 'center', gap: 1, flexWrap: 'wrap' }}>
            <span>{quote.number}</span>
            <QuoteStateChip state={quote.state} validUntil={quote.validUntil} />
          </Box>
        ) : (
          t('quotes:title')
        )}
      </DialogTitle>
      <DialogContent>
        {!quote && !error && <Box sx={{ display: 'flex', justifyContent: 'center', p: 3 }}><CircularProgress /></Box>}
        {error && <Alert severity="error">{error}</Alert>}
        {quote && totals && (
          <Stack spacing={2.5}>
            {lapsed && <Alert severity="warning">{t('quotes:detail.lapsed')}</Alert>}
            {quote.state === 'Rejected' && quote.rejectionReason && (
              <Alert severity="error">{t('quotes:detail.rejectedBecause', { reason: quote.rejectionReason })}</Alert>
            )}
            {quote.workOrderNumber && <Alert severity="success">{t('quotes:detail.workOrderMade', { number: quote.workOrderNumber })}</Alert>}

            <Box sx={{ display: 'grid', gridTemplateColumns: { xs: '110px 1fr', sm: '150px 1fr' }, rowGap: 1, columnGap: 2 }}>
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
              <Typography color="text.secondary">{t('quotes:detail.noLines')}</Typography>
            ) : fullScreen ? (
              // a phone has no room for a table: each line is a block
              <Stack divider={<Divider flexItem />} spacing={1}>
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
              <TableContainer>
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
              <Alert severity="info" icon={false}>
                {t('quotes:detail.deposit', {
                  rate: quote.requiredDepositPercentage,
                  required: money(quote.requiredDepositAmount),
                  paid: money(quote.depositPaidAmount),
                  remaining: money(remainingDeposit),
                })}
              </Alert>
            )}
            {!canEdit && <Typography variant="caption" color="text.secondary">{t('quotes:detail.readOnly')}</Typography>}
          </Stack>
        )}
      </DialogContent>

      <DialogActions sx={{ flexWrap: 'wrap', gap: 1, px: 3, pb: 2 }}>
        {canEdit && quote?.state === 'Draft' && (
          <>
            <Button color="error" onClick={() => setAction('delete')}>{t('quotes:actions.delete')}</Button>
            <Button onClick={() => setAction('edit')}>{t('quotes:actions.edit')}</Button>
          </>
        )}
        {canEdit && quote?.state === 'Issued' && <Button onClick={() => setAction('withdraw')}>{t('quotes:actions.withdraw')}</Button>}
        {canEdit && quote && quote.state !== 'Draft' && <Button onClick={() => setAction('copy')}>{t('quotes:actions.revise')}</Button>}
        {canEdit && quote?.state === 'Issued' && <Button color="error" onClick={() => setAction('reject')}>{t('quotes:actions.reject')}</Button>}
        <Button startIcon={<PrintIcon />} disabled={!quote} onClick={() => window.print()}>{t('quotes:actions.print')}</Button>
        {canEdit && quote?.state === 'Draft' && (
          <Button variant="contained" disabled={quote.items.length === 0} onClick={() => setAction('issue')}>{t('quotes:actions.issue')}</Button>
        )}
        {canEdit && quote?.state === 'Issued' && (
          <Button variant="contained" disabled={lapsed} onClick={() => setAction('accept')}>{t('quotes:actions.accept')}</Button>
        )}
        {canEdit && quote?.state === 'Accepted' && (
          <>
            {remainingDeposit > 0 && <Button onClick={() => setAction('deposit')}>{t('quotes:actions.deposit')}</Button>}
            {!quote.workOrderId && <Button variant="contained" onClick={() => setAction('convert')}>{t('quotes:actions.convert')}</Button>}
          </>
        )}
        <Button onClick={onClose}>{t('common:actions.close')}</Button>
      </DialogActions>

      {quote && action === 'edit' && <QuoteFormDialog quote={quote} onClose={() => setAction(null)} onSaved={changed} />}
      {quote && action === 'issue' && (
        <ConfirmDialog
          title={t('quotes:dialogs.issue.title')}
          body={t('quotes:dialogs.issue.body')}
          submitLabel={t('quotes:actions.issue')}
          onClose={() => setAction(null)}
          onConfirm={async () => changed(await quotesApi.issue(quote.id))}
        />
      )}
      {quote && action === 'accept' && <AcceptDialog quote={quote} onClose={() => setAction(null)} onDone={changed} />}
      {quote && action === 'reject' && <RejectDialog quote={quote} onClose={() => setAction(null)} onDone={changed} />}
      {quote && action === 'withdraw' && (
        <ConfirmDialog
          title={t('quotes:dialogs.withdraw.title')}
          body={t('quotes:dialogs.withdraw.body')}
          submitLabel={t('quotes:actions.withdraw')}
          color="warning"
          onClose={() => setAction(null)}
          onConfirm={async () => changed(await quotesApi.expire(quote.id))}
        />
      )}
      {quote && action === 'deposit' && <DepositDialog quote={quote} onClose={() => setAction(null)} onDone={changed} />}
      {quote && action === 'convert' && (
        <ConfirmDialog
          title={t('quotes:dialogs.convert.title')}
          body={t('quotes:dialogs.convert.body')}
          submitLabel={t('quotes:actions.convert')}
          onClose={() => setAction(null)}
          onConfirm={async () => {
            await quotesApi.convertToWorkOrder(quote.id)
            changed(await quotesApi.get(quote.id)) // now it knows its work order
          }}
        />
      )}
      {quote && action === 'copy' && (
        <ConfirmDialog
          title={t('quotes:dialogs.revise.title')}
          body={t('quotes:dialogs.revise.body')}
          submitLabel={t('quotes:actions.revise')}
          onClose={() => setAction(null)}
          onConfirm={async () => {
            const copy = await quotesApi.copy(quote.id)
            setAction(null)
            onChanged()
            onOpen(copy.id)
          }}
        />
      )}
      {quote && action === 'delete' && (
        <ConfirmDialog
          title={t('quotes:dialogs.delete.title')}
          body={t('quotes:dialogs.delete.body')}
          submitLabel={t('quotes:actions.delete')}
          color="warning"
          onClose={() => setAction(null)}
          onConfirm={async () => {
            await quotesApi.remove(quote.id)
            onChanged()
            onClose()
          }}
        />
      )}

      {/* the sheet for the customer: out of sight on screen, the only thing on the page when printed (see App.css) */}
      {quote && createPortal(<div className="quote-print-root"><QuoteDocument quote={quote} /></div>, document.body)}
    </Dialog>
  )
}

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

/** The customer said yes; a deposit can be asked for, as a share of the total. */
function AcceptDialog({ quote, onClose, onDone }: StepProps) {
  const { translate: t, lang } = useI18n()
  const [percentage, setPercentage] = useState(quote.requiredDepositPercentage)
  const invalid = percentage < 0 || percentage > 100
  const amount = fromCents(divRound(toCents(quote.total) * Math.round(percentage * 100), 10000))

  return (
    <FormDialog
      title={t('quotes:dialogs.accept.title')}
      submitLabel={t('quotes:actions.accept')}
      canSubmit={!invalid}
      onClose={onClose}
      onSubmit={async () => onDone(await quotesApi.accept(quote.id, percentage))}
    >
      <Typography>{t('quotes:dialogs.accept.body')}</Typography>
      <AmountField
        autoFocus
        label={t('quotes:dialogs.accept.percentage')}
        value={percentage}
        error={invalid}
        onChange={setPercentage}
        slotProps={{ input: { endAdornment: <InputAdornment position="end">{'%'}</InputAdornment> } }}
      />
      {!invalid && percentage > 0 && <Typography variant="body2" color="text.secondary">{t('quotes:dialogs.accept.amount', { amount: formatMoney(amount, lang) })}</Typography>}
    </FormDialog>
  )
}

/** The customer said no; the reason is kept with the quote. */
function RejectDialog({ quote, onClose, onDone }: StepProps) {
  const { translate: t } = useI18n()
  const [reason, setReason] = useState('')

  return (
    <FormDialog
      title={t('quotes:dialogs.reject.title')}
      submitLabel={t('quotes:actions.reject')}
      color="warning"
      canSubmit={reason.trim() !== ''}
      onClose={onClose}
      onSubmit={async () => onDone(await quotesApi.reject(quote.id, reason.trim()))}
    >
      <TextField autoFocus multiline minRows={2} label={t('quotes:dialogs.reject.reason')} value={reason} onChange={(event) => setReason(event.target.value)} />
    </FormDialog>
  )
}

/** Takes a deposit against the quote; all the deposits together cannot be more than the total. */
function DepositDialog({ quote, onClose, onDone }: StepProps) {
  const { translate: t, lang } = useI18n()
  const stillOwed = Math.max(0, quote.requiredDepositAmount - quote.depositPaidAmount)
  const room = fromCents(toCents(quote.total) - toCents(quote.depositPaidAmount))
  const [amount, setAmount] = useState(stillOwed)
  const invalid = amount <= 0 || toCents(amount) > toCents(room)

  return (
    <FormDialog
      title={t('quotes:dialogs.deposit.title')}
      submitLabel={t('quotes:actions.deposit')}
      canSubmit={!invalid}
      onClose={onClose}
      onSubmit={async () => onDone(await quotesApi.payDeposit(quote.id, amount))}
    >
      <Typography variant="body2" color="text.secondary">
        {t('quotes:dialogs.deposit.hint', { required: formatMoney(quote.requiredDepositAmount, lang), paid: formatMoney(quote.depositPaidAmount, lang), room: formatMoney(room, lang) })}
      </Typography>
      <AmountField autoFocus label={t('quotes:dialogs.deposit.amount')} value={amount} error={invalid && amount !== 0} onChange={setAmount} />
    </FormDialog>
  )
}

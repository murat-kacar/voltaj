import { useEffect, useState } from 'react'
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
  Stack,
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableRow,
  Typography,
} from '@mui/material'
import { customersApi, quickSalesApi, quotesApi, type Customer, type QuickSaleSummary, type QuoteSummary } from '../api'
import { errorText } from '../common/errors'
import { formatMoney } from '../common/format'
import { FormDialog } from '../common/FormDialog'
import { formatDate } from '../i18n/formatters'
import { useI18n } from '../i18n'
import { QuoteStateChip } from '../quotes/QuoteStateChip'
import { saleStatusKey, statusColor } from '../quickSaleUtils'
import { CustomerFormDialog } from './CustomerFormDialog'
import { SitesPanel } from './SitesPanel'

type Props = {
  id: string
  /** Managers can change a customer; everyone else only looks. */
  canEdit: boolean
  onClose: () => void
  /** The customer was changed, so the list behind is stale. */
  onChanged: () => void
}

/** One customer: their details, their addresses and devices, and what has been done with them. */
export function CustomerDetailDialog({ id, canEdit, onClose, onChanged }: Props) {
  const { translate: t, lang } = useI18n()
  const [customer, setCustomer] = useState<Customer | null>(null)
  const [error, setError] = useState('')
  const [dialog, setDialog] = useState<'edit' | 'convert' | 'toggle' | null>(null)

  useEffect(() => {
    let ignore = false
    customersApi
      .get(id)
      .then((loaded) => {
        if (!ignore) setCustomer(loaded)
      })
      .catch((reason: unknown) => {
        if (!ignore) setError(errorText(reason))
      })
    return () => {
      ignore = true
    }
  }, [id])

  const changed = (updated: Customer) => {
    setCustomer(updated)
    onChanged()
  }

  return (
    <Dialog open onClose={onClose} maxWidth="sm" fullWidth data-testid="dialog-36bc22">
      <DialogTitle>
        {customer ? (
          <Box sx={{ display: 'flex', alignItems: 'center', gap: 1, flexWrap: 'wrap' }}>
            <span>{customer.fullName}</span>
            <Chip size="small" color={customer.type === 'Active' ? 'success' : 'default'} label={t(`customers:type.${customer.type}`)} />
            {!customer.isActive && <Chip size="small" color="warning" label={t('customers:status.inactive')} />}
          </Box>
        ) : (
          t('customers:title')
        )}
      </DialogTitle>
      <DialogContent>
        {!customer && !error && <Box sx={{ display: 'flex', justifyContent: 'center', p: 3 }}><CircularProgress /></Box>}
        {error && <Alert severity="error">{error}</Alert>}
        {customer && (
          <Stack spacing={3} divider={<Divider flexItem />}>
            <Box component="section">
              <Typography variant="h6" component="h3" gutterBottom>{t('customers:detail.sections.info')}</Typography>
              <Stack spacing={2}>
                <Box sx={{ display: 'grid', gridTemplateColumns: '130px 1fr', rowGap: 1, columnGap: 2 }}>
                  <Typography color="text.secondary">{t('customers:table.phone')}</Typography>
                  <Typography>{customer.phone}</Typography>
                  <Typography color="text.secondary">{t('customers:table.email')}</Typography>
                  <Typography>{customer.email || '—'}</Typography>
                  <Typography color="text.secondary">{t('customers:table.taxNumber')}</Typography>
                  <Typography>{customer.taxNumber || '—'}</Typography>
                </Box>
                <Typography variant="caption" color="text.secondary">
                  {t('customers:detail.since', { date: formatDate(customer.createdAt, lang, { year: 'numeric', month: 'short', day: 'numeric' }) })}
                </Typography>

                {canEdit ? (
                  <Stack direction="row" spacing={1} sx={{ flexWrap: 'wrap', gap: 1 }}>
                    <Button variant="outlined" onClick={() => setDialog('edit')} data-testid="button-866e7b">{t('customers:detail.edit')}</Button>
                    {customer.type === 'Lead' && <Button variant="outlined" onClick={() => setDialog('convert')} data-testid="button-593386">{t('customers:detail.convert')}</Button>}
                    <Button variant="outlined" color={customer.isActive ? 'warning' : 'success'} onClick={() => setDialog('toggle')} data-testid="button-e8a7c2">
                      {customer.isActive ? t('customers:detail.deactivate') : t('customers:detail.activate')}
                    </Button>
                  </Stack>
                ) : (
                  <Typography variant="caption" color="text.secondary">{t('customers:detail.readOnly')}</Typography>
                )}
              </Stack>
            </Box>
            <Box component="section">
              <Typography variant="h6" component="h3" gutterBottom>{t('customers:detail.sections.addresses')}</Typography>
              <SitesPanel customer={customer} canEdit={canEdit} />
            </Box>
            <Box component="section">
              <Typography variant="h6" component="h3" gutterBottom>{t('customers:detail.sections.activity')}</Typography>
              <ActivityPanel customerId={customer.id} />
            </Box>
          </Stack>
        )}
      </DialogContent>
      <DialogActions>
        <Button variant="contained" onClick={onClose} data-testid="button-6478d8">{t('common:actions.close')}</Button>
      </DialogActions>

      {customer && dialog === 'edit' && (
        <CustomerFormDialog
          customer={customer}
          onClose={() => setDialog(null)}
          onSaved={(updated) => {
            setDialog(null)
            changed(updated)
          }}
        />
      )}
      {customer && dialog === 'convert' && (
        <FormDialog
          title={t('customers:detail.convertTitle')}
          submitLabel={t('customers:detail.convert')}
          onClose={() => setDialog(null)}
          onSubmit={async () => {
            changed(await customersApi.convertToActive(customer.id))
            setDialog(null)
          }}
        >
          <Typography>{t('customers:detail.convertHint')}</Typography>
        </FormDialog>
      )}
      {customer && dialog === 'toggle' && (
        <FormDialog
          title={customer.isActive ? t('customers:detail.deactivateTitle') : t('customers:detail.activate')}
          submitLabel={customer.isActive ? t('customers:detail.deactivate') : t('customers:detail.activate')}
          color={customer.isActive ? 'warning' : 'primary'}
          onClose={() => setDialog(null)}
          onSubmit={async () => {
            changed(customer.isActive ? await customersApi.deactivate(customer.id) : await customersApi.activate(customer.id))
            setDialog(null)
          }}
        >
          {customer.isActive && <Typography>{t('customers:detail.deactivateHint')}</Typography>}
        </FormDialog>
      )}
    </Dialog>
  )
}

/** What has been done with the customer: their latest quick sales and quotes. What a role may not see stays out. */
function ActivityPanel({ customerId }: { customerId: string }) {
  const { translate: t, lang } = useI18n()
  const [sales, setSales] = useState<QuickSaleSummary[] | null>(null)
  const [quotes, setQuotes] = useState<QuoteSummary[] | null>(null)

  useEffect(() => {
    let ignore = false
    quickSalesApi
      .list({ customerId, limit: 10 })
      .then((page) => {
        if (!ignore) setSales(page.items)
      })
      .catch(() => {
        if (!ignore) setSales([])
      })
    quotesApi
      .page({ customerId, limit: 10 })
      .then((page) => {
        if (!ignore) setQuotes(page.items)
      })
      .catch(() => {
        if (!ignore) setQuotes([])
      })
    return () => {
      ignore = true
    }
  }, [customerId])

  return (
    <Stack spacing={3}>
      <Box>
        <Typography variant="subtitle1" sx={{ fontWeight: 600 }} gutterBottom>{t('customers:activity.sales')}</Typography>
        {sales === null ? (
          <CircularProgress size={20} />
        ) : sales.length === 0 ? (
          <Typography variant="body2" color="text.secondary">{t('customers:activity.none')}</Typography>
        ) : (
          <Table size="small">
            <TableHead>
              <TableRow>
                <TableCell>{t('customers:activity.number')}</TableCell>
                <TableCell>{t('customers:activity.date')}</TableCell>
                <TableCell align="right">{t('customers:activity.total')}</TableCell>
                <TableCell>{t('customers:activity.status')}</TableCell>
              </TableRow>
            </TableHead>
            <TableBody>
              {sales.map((sale) => {
                const key = saleStatusKey(sale.status, sale.hasReturns)
                return (
                  <TableRow key={sale.id}>
                    <TableCell>{sale.saleNumber}</TableCell>
                    <TableCell>{formatDate(sale.soldAt, lang)}</TableCell>
                    <TableCell align="right">{formatMoney(sale.grandTotal, lang)}</TableCell>
                    <TableCell><Chip size="small" color={statusColor(key)} label={t(`common:sales.status.${key}`)} /></TableCell>
                  </TableRow>
                )
              })}
            </TableBody>
          </Table>
        )}
      </Box>

      <Box>
        <Typography variant="subtitle1" sx={{ fontWeight: 600 }} gutterBottom>{t('customers:activity.quotes')}</Typography>
        {quotes === null ? (
          <CircularProgress size={20} />
        ) : quotes.length === 0 ? (
          <Typography variant="body2" color="text.secondary">{t('customers:activity.none')}</Typography>
        ) : (
          <Table size="small">
            <TableHead>
              <TableRow>
                <TableCell>{t('customers:activity.number')}</TableCell>
                <TableCell>{t('common:fields.title')}</TableCell>
                <TableCell align="right">{t('customers:activity.total')}</TableCell>
                <TableCell>{t('customers:activity.status')}</TableCell>
              </TableRow>
            </TableHead>
            <TableBody>
              {quotes.map((quote) => (
                <TableRow key={quote.id}>
                  <TableCell>{quote.number}</TableCell>
                  <TableCell>{quote.title}</TableCell>
                  <TableCell align="right">{formatMoney(quote.total, lang)}</TableCell>
                  <TableCell><QuoteStateChip state={quote.state} validUntil={quote.validUntil} /></TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        )}
      </Box>
    </Stack>
  )
}

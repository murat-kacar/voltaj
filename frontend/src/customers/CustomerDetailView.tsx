import { useEffect, useMemo, useState } from 'react'
import { useParams, useNavigate } from 'react-router-dom'
import {
  Alert, Box, Button, Chip, CircularProgress, Divider, IconButton,
  Paper, Stack, Tab, Tabs, Typography,
} from '@mui/material'
import ArrowBackIcon from '@mui/icons-material/ArrowBack'
import EditIcon from '@mui/icons-material/Edit'
import AddIcon from '@mui/icons-material/Add'
import type { GridColDef } from '@mui/x-data-grid'
import {
  customersApi, paymentsApi, quotesApi, sessionRoles,
  type Customer, type CustomerSite, type PaymentRow, type QuoteSummary, type SalesInvoiceRow,
} from '../api'
import { PagedGrid } from '../common/PagedGrid'
import { usePagedQuery } from '../common/usePagedQuery'
import { formatDay, formatMoney } from '../common/format'
import { useI18n } from '../i18n'
import { CustomerFormDialog } from './CustomerFormDialog'
import { CreateWorkOrderModal } from '../CreateWorkOrderModal'
import { QuoteFormDialog } from '../quotes/QuoteFormDialog'

function InfoRow({ label, value }: { label: string; value: React.ReactNode }) {
  return (
    <Box sx={{ display: 'flex', gap: 2, py: 0.75 }}>
      <Typography variant="body2" color="text.secondary" sx={{ width: 140, flexShrink: 0 }}>{label}</Typography>
      <Typography variant="body2">{value}</Typography>
    </Box>
  )
}

function OverviewTab({ customer, canEdit, lang, t, onEdited }: {
  customer: Customer
  canEdit: boolean
  lang: 'en' | 'tr'
  t: (key: string) => string
  onEdited: (c: Customer) => void
}) {
  const [editing, setEditing] = useState(false)
  return (
    <Box>
      <Paper sx={{ p: 3 }}>
        <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start', mb: 2 }}>
          <Typography variant="subtitle1" sx={{ fontWeight: 600 }}>{t('customers:detail.sections.info')}</Typography>
          {canEdit && (
            <IconButton size="small" onClick={() => setEditing(true)} aria-label={t('customers:detail.edit')}>
              <EditIcon fontSize="small" />
            </IconButton>
          )}
        </Box>
        <InfoRow label={t('customers:page.info.phone')} value={customer.phone} />
        <InfoRow label={t('customers:page.info.email')} value={customer.email ?? t('customers:page.info.noEmail')} />
        {customer.taxNumber && <InfoRow label={t('customers:page.info.taxNumber')} value={customer.taxNumber} />}
        <InfoRow label={t('customers:page.info.type')} value={
          <Chip size="small" color={customer.type === 'Active' ? 'success' : 'default'} label={t(`customers:type.${customer.type}`)} />
        } />
        <InfoRow label={t('customers:page.info.status')} value={
          <Chip size="small" color={customer.isActive ? 'default' : 'warning'} variant={customer.isActive ? 'outlined' : 'filled'}
            label={customer.isActive ? t('customers:status.active') : t('customers:status.inactive')} />
        } />
        {customer.createdAt && (
          <InfoRow label={t('customers:page.info.added')} value={formatDay(customer.createdAt, lang)} />
        )}
      </Paper>
      {editing && (
        <CustomerFormDialog
          customer={customer}
          onClose={() => setEditing(false)}
          onSaved={(updated) => { setEditing(false); onEdited(updated) }}
        />
      )}
    </Box>
  )
}

function QuotesTab({ customerId, lang, t }: { customerId: string; lang: 'en' | 'tr'; t: (k: string) => string }) {
  const navigate = useNavigate()
  const query = usePagedQuery<QuoteSummary>(
    (limit, offset) => quotesApi.page({ customerId, limit, offset }),
    customerId,
  )
  const columns = useMemo<GridColDef<QuoteSummary>[]>(() => [
    { field: 'number', headerName: t('customers:activity.number'), width: 140 },
    { field: 'title', headerName: t('common:fields.title'), flex: 1, minWidth: 200 },
    { field: 'state', headerName: t('customers:activity.status'), width: 130,
      renderCell: (p) => <Chip size="small" label={p.row.state} /> },
    { field: 'total', headerName: t('customers:activity.total'), width: 130, align: 'right', headerAlign: 'right',
      renderCell: (p) => formatMoney(p.row.total, lang) },
    { field: 'createdAt', headerName: t('customers:activity.date'), width: 130,
      renderCell: (p) => formatDay(p.row.createdAt, lang) },
  ], [t, lang])
  return (
    <PagedGrid
      columns={columns}
      query={query}
      emptyText={t('customers:page.noQuotes')}
      onRowClick={(row) => navigate('/quotes/' + row.id)}
    />
  )
}

function InvoicesTab({ customerId, lang, t }: { customerId: string; lang: 'en' | 'tr'; t: (k: string) => string }) {
  const navigate = useNavigate()
  const query = usePagedQuery<SalesInvoiceRow>(
    (limit, offset) => paymentsApi.listInvoices({ customerId, limit, offset }),
    customerId,
  )
  const columns = useMemo<GridColDef<SalesInvoiceRow>[]>(() => [
    { field: 'invoiceNumber', headerName: t('payments:table.invoiceNumber'), width: 150 },
    { field: 'grandTotal', headerName: t('payments:table.grandTotal'), width: 130, align: 'right', headerAlign: 'right',
      renderCell: (p) => formatMoney(p.row.grandTotal, lang) },
    { field: 'paidAmount', headerName: t('payments:table.paid'), width: 130, align: 'right', headerAlign: 'right',
      renderCell: (p) => formatMoney(p.row.paidAmount, lang) },
    { field: 'remainingAmount', headerName: t('payments:table.remaining'), width: 130, align: 'right', headerAlign: 'right',
      renderCell: (p) => formatMoney(p.row.remainingAmount, lang) },
    { field: 'invoiceDate', headerName: t('payments:table.invoiceDate'), width: 130,
      renderCell: (p) => formatDay(p.row.invoiceDate, lang) },
  ], [t, lang])
  return (
    <PagedGrid
      columns={columns}
      query={query}
      emptyText={t('customers:page.noInvoices')}
      onRowClick={(row) => navigate('/invoices/' + row.id, { state: { invoice: row } })}
    />
  )
}

function PaymentsTab({ customerId, lang, t }: { customerId: string; lang: 'en' | 'tr'; t: (k: string) => string }) {
  const query = usePagedQuery<PaymentRow>(
    (limit, offset) => paymentsApi.list({ customerId, limit, offset }),
    customerId,
  )
  const columns = useMemo<GridColDef<PaymentRow>[]>(() => [
    { field: 'amount', headerName: t('payments:table.amount'), width: 130, align: 'right', headerAlign: 'right',
      renderCell: (p) => formatMoney(p.row.amount, lang) },
    { field: 'paymentMethod', headerName: t('payments:table.method'), width: 140 },
    { field: 'paymentDate', headerName: t('payments:table.paymentDate'), width: 130,
      renderCell: (p) => formatDay(p.row.paymentDate, lang) },
  ], [t, lang])
  return <PagedGrid columns={columns} query={query} emptyText={t('customers:page.noPayments')} />
}

function SitesTab({ sites, t }: { sites: CustomerSite[]; t: (k: string) => string }) {
  if (sites.length === 0) return <Typography color="text.secondary" sx={{ p: 2 }}>{t('customers:page.noSites')}</Typography>
  return (
    <Stack spacing={2}>
      {sites.map(site => (
        <Paper key={site.id} variant="outlined" sx={{ p: 2 }}>
          <Typography sx={{ fontWeight: 600 }}>{site.name}</Typography>
          <Typography variant="body2" color="text.secondary">{site.address}</Typography>
          {site.assets.length > 0 && (
            <Box sx={{ mt: 1, pl: 1, borderLeft: 2, borderColor: 'divider' }}>
              {site.assets.map(a => (
                <Typography key={a.id} variant="body2" sx={{ py: 0.25 }}>
                  {a.name}{a.serialNumber ? ` — S/N ${a.serialNumber}` : ''}
                </Typography>
              ))}
            </Box>
          )}
        </Paper>
      ))}
    </Stack>
  )
}

export function CustomerDetailView() {
  const { id } = useParams<{ id: string }>()
  const navigate = useNavigate()
  const { translate: t, lang } = useI18n()
  const canEdit = useMemo(() => sessionRoles().some(r => r === 'Admin' || r === 'Manager'), [])

  const [customer, setCustomer] = useState<Customer | null>(null)
  const [sites, setSites] = useState<CustomerSite[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')
  const [tab, setTab] = useState(0)
  const [showNewWO, setShowNewWO] = useState(false)
  const [showNewQuote, setShowNewQuote] = useState(false)

  useEffect(() => {
    if (!id) return
    setLoading(true)
    Promise.all([customersApi.get(id), customersApi.sites(id)])
      .then(([c, s]) => { setCustomer(c); setSites(s); setLoading(false) })
      .catch(() => { setError(t('customers:page.loadFailed')); setLoading(false) })
  }, [id, t])

  if (loading) return <Box sx={{ display: 'flex', justifyContent: 'center', p: 6 }}><CircularProgress /></Box>
  if (error || !customer) return (
    <Box sx={{ p: 3 }}>
      <Alert severity="error">{error || t('customers:page.loadFailed')}</Alert>
      <Button sx={{ mt: 2 }} startIcon={<ArrowBackIcon />} onClick={() => navigate('/customers')}>{t('customers:page.back')}</Button>
    </Box>
  )

  const tabs = [
    t('customers:page.tabs.overview'),
    t('customers:page.tabs.quotes'),
    t('customers:page.tabs.invoices'),
    t('customers:page.tabs.payments'),
    t('customers:page.tabs.sites'),
  ]

  return (
    <Box>
      <Box sx={{ display: 'flex', alignItems: 'center', gap: 1, mb: 2 }}>
        <Button size="small" startIcon={<ArrowBackIcon />} onClick={() => navigate('/customers')}>
          {t('customers:page.back')}
        </Button>
      </Box>

      <Box sx={{ display: 'flex', alignItems: 'flex-start', gap: 2, mb: 3, flexWrap: 'wrap' }}>
        <Box sx={{ flex: 1, minWidth: 200 }}>
          <Typography variant="overline" color="text.secondary">{t(`customers:type.${customer.type}`)}</Typography>
          <Typography variant="h4">{customer.fullName}</Typography>
        </Box>
        <Stack direction="row" spacing={1} sx={{ alignItems: 'center', flexWrap: 'wrap' }}>
          <Chip
            size="small"
            color={customer.isActive ? 'default' : 'warning'}
            variant={customer.isActive ? 'outlined' : 'filled'}
            label={customer.isActive ? t('customers:status.active') : t('customers:status.inactive')}
          />
          {canEdit && (
            <>
              <Button size="small" variant="outlined" startIcon={<AddIcon />} onClick={() => setShowNewWO(true)}>
                {t('customers:page.newWorkOrder')}
              </Button>
              <Button size="small" variant="outlined" startIcon={<AddIcon />} onClick={() => setShowNewQuote(true)}>
                {t('customers:page.newQuote')}
              </Button>
            </>
          )}
        </Stack>
      </Box>

      <Divider sx={{ mb: 0 }} />
      <Tabs value={tab} onChange={(_, v) => setTab(v)} variant="scrollable" scrollButtons="auto" sx={{ mb: 3 }}>
        {tabs.map((label, i) => <Tab key={i} label={label} />)}
      </Tabs>

      {tab === 0 && <OverviewTab customer={customer} canEdit={canEdit} lang={lang} t={t as (k: string) => string} onEdited={setCustomer} />}
      {tab === 1 && <QuotesTab customerId={customer.id} lang={lang} t={t as (k: string) => string} />}
      {tab === 2 && <InvoicesTab customerId={customer.id} lang={lang} t={t as (k: string) => string} />}
      {tab === 3 && <PaymentsTab customerId={customer.id} lang={lang} t={t as (k: string) => string} />}
      {tab === 4 && <SitesTab sites={sites} t={t as (k: string) => string} />}

      {showNewWO && (
        <CreateWorkOrderModal
          initialCustomer={customer}
          onClose={() => setShowNewWO(false)}
          onSuccess={() => { setShowNewWO(false); navigate('/work-orders') }}
        />
      )}
      {showNewQuote && (
        <QuoteFormDialog
          quote={null}
          initialCustomer={customer}
          onClose={() => setShowNewQuote(false)}
          onSaved={(created) => { setShowNewQuote(false); navigate('/quotes/' + created.id) }}
        />
      )}
    </Box>
  )
}

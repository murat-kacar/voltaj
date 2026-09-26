import { useEffect, useMemo, useState } from 'react'
import { useParams, useNavigate } from 'react-router-dom'
import {
  Alert, Box, Button, Chip, CircularProgress, Divider, IconButton,
  Paper, Stack, Tab, Tabs, Typography, TextField, MenuItem
} from '@mui/material'
import ArrowBackIcon from '@mui/icons-material/ArrowBack'
import EditIcon from '@mui/icons-material/Edit'
import type { GridColDef } from '@mui/x-data-grid'
import {
  customersApi, paymentsApi, sessionRoles,
  type Customer, type CustomerSite, type PaymentRow, type SalesInvoiceRow,
} from '../../api'
import { PagedGrid } from '../../common/PagedGrid'
import { usePagedQuery } from '../../common/usePagedQuery'
import { formatDay, formatMoney } from '../../common/format'
import { useI18n } from '../../i18n'
import { CustomerFormDialog } from './CustomerFormDialog'

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
            <IconButton size="small" onClick={() => setEditing(true)} aria-label={t('customers:detail.edit')} data-testid="iconbutton-899630">
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
  const [receiving, setReceiving] = useState(false)
  return (
    <Box>
      <Box sx={{ mb: 2, display: 'flex', justifyContent: 'flex-end' }}>
        <Button variant="contained" onClick={() => setReceiving(true)}>Tahsilat Ekle (Receive Payment)</Button>
      </Box>
      <PagedGrid columns={columns} query={query} emptyText={t('customers:page.noPayments')} />
      {receiving && <ReceivePaymentDialog customerId={customerId} onClose={() => setReceiving(false)} onSaved={() => { setReceiving(false); query.reload() }} />}
    </Box>
  )
}

function ReceivePaymentDialog({ customerId, onClose, onSaved }: { customerId: string, onClose: () => void, onSaved: () => void }) {
  const [amount, setAmount] = useState(0)
  const [method, setMethod] = useState('Cash')
  const [saving, setSaving] = useState(false)

  const handleSave = async () => {
    if (amount <= 0) return
    setSaving(true)
    try {
      await paymentsApi.create({ customerId, amount, paymentMethod: method, paymentDate: new Date().toISOString() })
      onSaved()
    } catch {
      // error
    } finally {
      setSaving(false)
    }
  }

  return (
    <CustomerFormDialogWrapper title="Tahsilat Al" onClose={onClose} onSubmit={handleSave} canSubmit={amount > 0 && !saving}>
      <Stack spacing={3} sx={{ mt: 1, minWidth: 300 }}>
        <TextField
          type="number"
          label="Tutar (₺)"
          value={amount || ''}
          onChange={(e: React.ChangeEvent<HTMLInputElement>) => setAmount(Number(e.target.value))}
          fullWidth
          required
        />
        <TextField select label="Ödeme Yöntemi" value={method} onChange={(e: React.ChangeEvent<HTMLInputElement>) => setMethod(e.target.value)} fullWidth>
          <MenuItem value="Cash">Nakit</MenuItem>
          <MenuItem value="Card">Kredi Kartı</MenuItem>
          <MenuItem value="BankTransfer">Havale / EFT</MenuItem>
        </TextField>
      </Stack>
    </CustomerFormDialogWrapper>
  )
}

function CustomerFormDialogWrapper({ title, onClose, onSubmit, canSubmit, children }: any) {
  return (
    <Paper sx={{ position: 'fixed', top: '50%', left: '50%', transform: 'translate(-50%, -50%)', zIndex: 1300, p: 3, minWidth: 350, boxShadow: 24, borderRadius: 2 }}>
      <Typography variant="h6" sx={{ mb: 2 }}>{title}</Typography>
      {children}
      <Stack direction="row" spacing={2} sx={{ mt: 3, justifyContent: 'flex-end' }}>
        <Button onClick={onClose}>İptal</Button>
        <Button variant="contained" onClick={onSubmit} disabled={!canSubmit}>Kaydet</Button>
      </Stack>
    </Paper>
  )
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

  useEffect(() => {
    if (!id) return
    // oxlint-disable-next-line react/set-state-in-effect
    setLoading(true)
    Promise.all([customersApi.get(id), customersApi.sites(id)])
      .then(([c, s]) => { setCustomer(c); setSites(s); setLoading(false) })
      .catch(() => { setError(t('customers:page.loadFailed')); setLoading(false) })
  }, [id, t])

  if (loading) return <Box sx={{ display: 'flex', justifyContent: 'center', p: 6 }}><CircularProgress /></Box>
  if (error || !customer) return (
    <Box sx={{ p: 3 }}>
      <Alert severity="error">{error || t('customers:page.loadFailed')}</Alert>
      <Button sx={{ mt: 2 }} startIcon={<ArrowBackIcon />} onClick={() => navigate('/customers')} data-testid="button-8db648">{t('customers:page.back')}</Button>
    </Box>
  )

  const tabs = [
    t('customers:page.tabs.overview'),
    t('customers:page.tabs.invoices'),
    t('customers:page.tabs.payments'),
    t('customers:page.tabs.sites'),
  ]

  return (
    <Box>
      <Box sx={{ display: 'flex', alignItems: 'center', gap: 1, mb: 2 }}>
        <Button size="small" startIcon={<ArrowBackIcon />} onClick={() => navigate('/customers')} data-testid="button-4b5194">
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
        </Stack>
      </Box>

      <Divider sx={{ mb: 0 }} />
      <Tabs value={tab} onChange={(_, v) => setTab(v)} variant="scrollable" scrollButtons="auto" sx={{ mb: 3 }}>
        {tabs.map((label, i) => <Tab key={i} label={label}  data-testid="tab-9522ae" />)}
      </Tabs>

      {tab === 0 && <OverviewTab customer={customer} canEdit={canEdit} lang={lang} t={t as (k: string) => string} onEdited={setCustomer} />}
      {tab === 1 && <InvoicesTab customerId={customer.id} lang={lang} t={t as (k: string) => string} />}
      {tab === 2 && <PaymentsTab customerId={customer.id} lang={lang} t={t as (k: string) => string} />}
      {tab === 3 && <SitesTab sites={sites} t={t as (k: string) => string} />}


    </Box>
  )
}

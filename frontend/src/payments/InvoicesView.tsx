import { useMemo, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { Box } from '@mui/material'
import type { GridColDef } from '@mui/x-data-grid'
import { paymentsApi, type Customer, type SalesInvoiceRow } from '../api'
import { formatDay, formatMoney } from '../common/format'
import { PageHeader } from '../common/PageHeader'
import { PagedGrid } from '../common/PagedGrid'
import { usePagedQuery } from '../common/usePagedQuery'
import { CustomerPicker } from '../customers/CustomerPicker'
import { useI18n } from '../i18n'

/** Every invoice, newest first. Picking a customer narrows the list to theirs; clearing the pick brings everyone's back. */
export function InvoicesView() {
  const { translate: t, lang } = useI18n()
  const navigate = useNavigate()
  const [customer, setCustomer] = useState<Customer | null>(null)

  const query = usePagedQuery<SalesInvoiceRow>(
    (limit, offset) => paymentsApi.listInvoices({ customerId: customer?.id, limit, offset }),
    customer?.id ?? '',
  )

  const columns = useMemo<GridColDef<SalesInvoiceRow>[]>(() => {
    const money = (field: 'grandTotal' | 'paidAmount' | 'remainingAmount', headerName: string): GridColDef<SalesInvoiceRow> => ({
      field,
      headerName,
      width: 140,
      align: 'right',
      headerAlign: 'right',
      renderCell: (params) => formatMoney(params.row[field], lang),
    })
    return [
      { field: 'invoiceNumber', headerName: t('payments:table.invoiceNumber'), width: 150 },
      { field: 'customerName', headerName: t('payments:table.customer'), flex: 1, minWidth: 180 },
      money('grandTotal', t('payments:table.grandTotal')),
      money('paidAmount', t('payments:table.paid')),
      money('remainingAmount', t('payments:table.remaining')),
      { field: 'invoiceDate', headerName: t('payments:table.invoiceDate'), width: 130, renderCell: (params) => formatDay(params.row.invoiceDate, lang) },
    ]
  }, [t, lang])

  return (
    <Box>
      <PageHeader overline={t('payments:eyebrow')} title={t('payments:invoicesTitle')} />
      <Box sx={{ mb: 2, maxWidth: 420 }}>
        <CustomerPicker value={customer} onChange={setCustomer} label={t('payments:filterByCustomer')} />
      </Box>
      <PagedGrid
        columns={columns}
        query={query}
        emptyText={t('payments:emptyInvoices')}
        onRowClick={(row) => navigate('/invoices/' + row.id, { state: { invoice: row } })}
      />
    </Box>
  )
}

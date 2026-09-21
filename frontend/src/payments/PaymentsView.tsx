import { useMemo, useState } from 'react'
import { Box, Button, MenuItem, TextField } from '@mui/material'
import AddIcon from '@mui/icons-material/Add'
import type { GridColDef } from '@mui/x-data-grid'
import { paymentsApi, type Customer, type PaymentRow } from '../api'
import { AmountField } from '../common/AmountField'
import { FormDialog } from '../common/FormDialog'
import { formatDay, formatMoney } from '../common/format'
import { PageHeader } from '../common/PageHeader'
import { PagedGrid } from '../common/PagedGrid'
import { usePagedQuery } from '../common/usePagedQuery'
import { CustomerPicker } from '../customers/CustomerPicker'
import { useI18n } from '../i18n'

const METHODS = ['Cash', 'Card', 'BankTransfer'] as const

/** Every payment, newest first. Picking a customer narrows the list to theirs; clearing the pick brings everyone's back. */
export function PaymentsView() {
  const { translate: t, lang } = useI18n()
  const [customer, setCustomer] = useState<Customer | null>(null)
  const [recording, setRecording] = useState(false)

  const query = usePagedQuery<PaymentRow>(
    (limit, offset) => paymentsApi.list({ customerId: customer?.id, limit, offset }),
    customer?.id ?? '',
  )

  const columns = useMemo<GridColDef<PaymentRow>[]>(
    () => [
      { field: 'customerName', headerName: t('payments:table.customer'), flex: 1, minWidth: 180 },
      {
        field: 'amount',
        headerName: t('payments:table.amount'),
        width: 150,
        align: 'right',
        headerAlign: 'right',
        renderCell: (params) => formatMoney(params.row.amount, lang),
      },
      { field: 'paymentMethod', headerName: t('payments:table.method'), width: 160, renderCell: (params) => t(`common:sales.methods.${params.row.paymentMethod}`, { defaultValue: params.row.paymentMethod }) },
      { field: 'paymentDate', headerName: t('payments:table.paymentDate'), width: 130, renderCell: (params) => formatDay(params.row.paymentDate, lang) },
    ],
    [t, lang],
  )

  return (
    <Box>
      <PageHeader
        overline={t('payments:eyebrow')}
        title={t('payments:title')}
        actions={
          <Button variant="contained" startIcon={<AddIcon />} onClick={() => setRecording(true)} data-testid="payments-record">
            {t('payments:actions.recordPayment')}
          </Button>
        }
      />
      <Box sx={{ mb: 2, maxWidth: 420 }}>
        <CustomerPicker value={customer} onChange={setCustomer} label={t('payments:filterByCustomer')} />
      </Box>
      <PagedGrid columns={columns} query={query} emptyText={t('payments:emptyPayments')} />

      {recording && (
        <RecordPaymentDialog
          initialCustomer={customer}
          onClose={() => setRecording(false)}
          onSaved={() => {
            setRecording(false)
            query.reload()
          }}
        />
      )}
    </Box>
  )
}

function RecordPaymentDialog({ initialCustomer, onClose, onSaved }: { initialCustomer: Customer | null; onClose: () => void; onSaved: () => void }) {
  const { translate: t } = useI18n()
  const [customer, setCustomer] = useState<Customer | null>(initialCustomer)
  const [amount, setAmount] = useState(0)
  const [method, setMethod] = useState<string>('Cash')
  const [date, setDate] = useState(new Date().toISOString().slice(0, 10))

  return (
    <FormDialog
      title={t('payments:form.title')}
      submitLabel={t('payments:form.submit')}
      canSubmit={customer !== null && amount > 0 && date !== ''}
      onClose={onClose}
      onSubmit={async () => {
        if (!customer) return
        await paymentsApi.create({ customerId: customer.id, amount, paymentMethod: method, paymentDate: date })
        onSaved()
      }}
    >
      <CustomerPicker value={customer} onChange={setCustomer} label={t('payments:form.customer')} size="medium" />
      <AmountField autoFocus={initialCustomer !== null} label={t('payments:form.amount')} value={amount} onChange={setAmount} />
      <TextField select label={t('payments:form.method')} value={method} onChange={(event) => setMethod(event.target.value)}>
        {METHODS.map((option) => <MenuItem key={option} value={option}>{t(`common:sales.methods.${option}`)}</MenuItem>)}
      </TextField>
      <TextField type="date" label={t('payments:form.date')} value={date} onChange={(event) => setDate(event.target.value)} slotProps={{ inputLabel: { shrink: true } }} />
    </FormDialog>
  )
}

import { useState } from 'react'
import { TextField } from '@mui/material'
import { customersApi, type Customer } from '../api'
import { FormDialog } from '../common/FormDialog'
import { useI18n } from '../i18n'

const looksLikeAnEmail = (value: string) => /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(value)

/** A new customer, or the details of an existing one. Only the name and the phone are required: a customer met at the door often has no email. */
export function CustomerFormDialog({ customer, onClose, onSaved }: { customer: Customer | null; onClose: () => void; onSaved: (customer: Customer) => void }) {
  const { translate: t } = useI18n()
  const [fullName, setFullName] = useState(customer?.fullName ?? '')
  const [phone, setPhone] = useState(customer?.phone ?? '')
  const [email, setEmail] = useState(customer?.email ?? '')
  const [taxNumber, setTaxNumber] = useState(customer?.taxNumber ?? '')
  const emailInvalid = email.trim() !== '' && !looksLikeAnEmail(email.trim())

  return (
    <FormDialog
      title={customer ? t('customers:form.editTitle') : t('customers:form.newTitle')}
      submitLabel={t('common:actions.save')}
      canSubmit={fullName.trim() !== '' && phone.trim() !== '' && !emailInvalid}
      onClose={onClose}
      onSubmit={async () => {
        const payload = { fullName: fullName.trim(), phone: phone.trim(), email: email.trim() || undefined, taxNumber: taxNumber.trim() || undefined }
        onSaved(customer ? await customersApi.update(customer.id, payload) : await customersApi.create(payload))
      }}
    >
      <TextField required autoFocus label={t('customers:form.fullName')} value={fullName} onChange={(event) => setFullName(event.target.value)} />
      <TextField required label={t('customers:form.phone')} value={phone} onChange={(event) => setPhone(event.target.value)} />
      <TextField
        label={t('customers:form.email')}
        value={email}
        error={emailInvalid}
        helperText={emailInvalid ? t('customers:form.emailInvalid') : undefined}
        onChange={(event) => setEmail(event.target.value)}
      />
      <TextField label={t('customers:form.taxNumber')} value={taxNumber} onChange={(event) => setTaxNumber(event.target.value)} />
    </FormDialog>
  )
}

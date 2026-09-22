import { useState } from 'react'
import { Alert, Box, Button, Dialog, DialogActions, DialogContent, DialogTitle, TextField, Typography } from '@mui/material'
import { useI18n } from './i18n'
import { workOrdersApi, type Customer } from './api'
import { CustomerPicker } from './customers/CustomerPicker'

export function CreateWorkOrderModal({ onClose, onSuccess, initialCustomer }: { onClose: () => void; onSuccess: () => void; initialCustomer?: Customer }) {
  const { translate: t } = useI18n()
  const [customer, setCustomer] = useState<Customer | null>(initialCustomer ?? null)
  const [title, setTitle] = useState('')
  const [error, setError] = useState('')
  const [loading, setLoading] = useState(false)

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    if (!customer) { setError(t('workOrders:validation.customerRequired')); return }
    if (!title.trim()) { setError(t('workOrders:validation.titleRequired')); return }
    setLoading(true)
    setError('')
    try {
      await workOrdersApi.create({ customerId: customer.id, title: title.trim() })
      onSuccess()
    } catch (err) {
      setError(err instanceof Error ? err.message : t('workOrders:errors.createFailed'))
    } finally {
      setLoading(false)
    }
  }

  return (
    <Dialog open={true} onClose={onClose} maxWidth="sm" fullWidth data-testid="dialog-c7794f">
      <DialogTitle>
        <Typography variant="h6">{t('workOrders:form.title')}</Typography>
        <Typography variant="body2" color="text.secondary">{t('workOrders:form.subtitle')}</Typography>
      </DialogTitle>
      <Box component="form" onSubmit={handleSubmit}>
        <DialogContent dividers sx={{ display: 'flex', flexDirection: 'column', gap: 2 }}>
          {error && <Alert severity="error">{error}</Alert>}
          <CustomerPicker
            value={customer}
            onChange={setCustomer}
            label={t('workOrders:form.customer')}
          />
          <TextField
            required
            fullWidth
            label={t('workOrders:form.orderTitle')}
            placeholder={t('workOrders:form.orderTitlePlaceholder')}
            value={title}
            onChange={(e) => setTitle(e.target.value)}
           data-testid="textfield-6f2d1c" />
        </DialogContent>
        <DialogActions>
          <Button onClick={onClose} color="inherit" data-testid="button-3ebf56">{t('workOrders:form.cancel')}</Button>
          <Button type="submit" variant="contained" disabled={loading} data-testid="button-f9e246">
            {loading ? t('workOrders:form.creating') : t('workOrders:form.submit')}
          </Button>
        </DialogActions>
      </Box>
    </Dialog>
  )
}

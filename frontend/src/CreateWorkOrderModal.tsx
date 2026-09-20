import { useState } from 'react'
import { Alert, Box, Button, Dialog, DialogActions, DialogContent, DialogTitle, TextField, Typography } from '@mui/material'
import { useI18n } from './i18n'
import { workOrdersApi, type Customer } from './api'
import { CustomerPicker } from './customers/CustomerPicker'

export function CreateWorkOrderModal({ onClose, onSuccess }: { onClose: () => void; onSuccess: () => void }) {
  const { translate: t } = useI18n()
  const [customer, setCustomer] = useState<Customer | null>(null)
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
    <Dialog open={true} onClose={onClose} maxWidth="sm" fullWidth>
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
          />
        </DialogContent>
        <DialogActions>
          <Button onClick={onClose} color="inherit">{t('workOrders:form.cancel')}</Button>
          <Button type="submit" variant="contained" disabled={loading}>
            {loading ? t('workOrders:form.creating') : t('workOrders:form.submit')}
          </Button>
        </DialogActions>
      </Box>
    </Dialog>
  )
}

import { useState } from 'react'
import { Dialog, DialogTitle, DialogContent, DialogActions, TextField, Button, Box } from '@mui/material'
import { useI18n } from './i18n'
import { customersApi } from './api'

export function CreateCustomerModal({ onClose, onSuccess }: { onClose: () => void, onSuccess: () => void }) {
  const { translate: t } = useI18n()
  const [name, setName] = useState('')
  const [email, setEmail] = useState('')
  const [phone, setPhone] = useState('')
  const [loading, setLoading] = useState(false)

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    setLoading(true)
    try {
      await customersApi.create({ name, email, phone })
      onSuccess()
    } catch {
      // Ignored for demo
    } finally {
      setLoading(false)
    }
  }

  return (
    <Dialog open={true} onClose={onClose} maxWidth="sm" fullWidth>
      <DialogTitle>{t('common:modals.newCustomer')}</DialogTitle>
      <Box component="form" onSubmit={handleSubmit}>
        <DialogContent dividers sx={{ display: 'flex', flexDirection: 'column', gap: 2 }}>
          <TextField
            required
            fullWidth
            label={t('common:modals.nameCompany')}
            value={name}
            onChange={e => setName(e.target.value)}
          />
          <TextField
            fullWidth
            type="email"
            label={t('common:modals.email')}
            value={email}
            onChange={e => setEmail(e.target.value)}
          />
          <TextField
            fullWidth
            label={t('common:modals.phone')}
            value={phone}
            onChange={e => setPhone(e.target.value)}
          />
        </DialogContent>
        <DialogActions>
          <Button onClick={onClose} color="inherit">{t('common:modals.cancel')}</Button>
          <Button type="submit" variant="contained" disabled={loading}>
            {loading ? '...' : t('common:modals.create')}
          </Button>
        </DialogActions>
      </Box>
    </Dialog>
  )
}

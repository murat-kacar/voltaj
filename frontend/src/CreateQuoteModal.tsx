import { useState } from 'react'
import { Dialog, DialogTitle, DialogContent, DialogActions, TextField, Button, Box } from '@mui/material'
import { useI18n } from './i18n'
import { quotesApi } from './api'

export function CreateQuoteModal({ onClose, onSuccess }: { onClose: () => void, onSuccess: () => void }) {
  const { translate: t } = useI18n()
  const [title, setTitle] = useState('')
  const [customerId, setCustomerId] = useState('')
  const [totalAmount, setTotalAmount] = useState('')
  const [loading, setLoading] = useState(false)

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    setLoading(true)
    try {
      await quotesApi.create({
        title,
        customerId,
        number: `QT-${Math.floor(Math.random() * 10000)}`,
        status: 'Draft',
        totalAmount: Number(totalAmount) || 0
      })
      onSuccess()
    } catch {
      // Ignored for demo
    } finally {
      setLoading(false)
    }
  }

  return (
    <Dialog open={true} onClose={onClose} maxWidth="sm" fullWidth>
      <DialogTitle>{t('common:modals.newQuote')}</DialogTitle>
      <Box component="form" onSubmit={handleSubmit}>
        <DialogContent dividers sx={{ display: 'flex', flexDirection: 'column', gap: 2 }}>
          <TextField
            required
            fullWidth
            label={t('common:modals.quoteTitle')}
            value={title}
            onChange={e => setTitle(e.target.value)}
          />
          <TextField
            required
            fullWidth
            label={t('common:fields.customer')}
            value={customerId}
            onChange={e => setCustomerId(e.target.value)}
          />
          <TextField
            required
            fullWidth
            type="number"
            label={t('common:modals.amount')}
            value={totalAmount}
            onChange={e => setTotalAmount(e.target.value)}
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

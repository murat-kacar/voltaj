import { useState } from 'react'
import { Dialog, DialogTitle, DialogContent, DialogActions, TextField, Button, Box } from '@mui/material'
import { useI18n } from './i18n'
import { workOrdersApi } from './api'

export function CreateWorkOrderModal({ onClose, onSuccess }: { onClose: () => void, onSuccess: () => void }) {
  const { translate: t } = useI18n()
  const [title, setTitle] = useState('')
  const [customerId, setCustomerId] = useState('')
  const [description, setDescription] = useState('')
  const [loading, setLoading] = useState(false)

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    setLoading(true)
    try {
      await workOrdersApi.create({
        title,
        customerId,
        description,
        number: `WO-${Math.floor(Math.random() * 10000)}`,
        status: 'In Progress'
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
      <DialogTitle>{t('common:modals.newWorkOrder')}</DialogTitle>
      <Box component="form" onSubmit={handleSubmit}>
        <DialogContent dividers sx={{ display: 'flex', flexDirection: 'column', gap: 2 }}>
          <TextField
            required
            fullWidth
            label={t('common:modals.woTitle')}
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
            fullWidth
            multiline
            rows={3}
            label={t('common:modals.description')}
            value={description}
            onChange={e => setDescription(e.target.value)}
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

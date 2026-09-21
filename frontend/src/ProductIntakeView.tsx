import { useState } from 'react'
import {
  Box,
  Typography,
  Paper,
  TextField,
  Button,
  FormControlLabel,
  Switch,
  CircularProgress,
  Alert
} from '@mui/material'
import PhotoCameraIcon from '@mui/icons-material/PhotoCamera'
import CheckCircleIcon from '@mui/icons-material/CheckCircle'
import { useI18n } from './i18n'
import { workOrdersApi, type Customer } from './api'
import { CustomerPicker } from './customers/CustomerPicker'

export function ProductIntakeView() {
  const { translate: t } = useI18n()

  const [submitting, setSubmitting] = useState(false)
  const [success, setSuccess] = useState(false)
  const [submitError, setSubmitError] = useState('')

  const [customer, setCustomer] = useState<Customer | null>(null)
  const [deviceModel, setDeviceModel] = useState('')
  const [serialNumber, setSerialNumber] = useState('')
  const [complaint, setComplaint] = useState('')
  const [hasWarranty, setHasWarranty] = useState(false)
  const [photo, setPhoto] = useState<File | null>(null)

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    if (!customer) return
    setSubmitting(true)
    setSubmitError('')

    const warrantyNote = hasWarranty ? ' — Garanti kapsamında' : ''
    const title = `Cihaz Kabulü: ${deviceModel} (S/N: ${serialNumber})${warrantyNote}`

    try {
      await workOrdersApi.create({ customerId: customer.id, title })
      setSuccess(true)
      setTimeout(() => {
        setSuccess(false)
        setCustomer(null)
        setDeviceModel('')
        setSerialNumber('')
        setComplaint('')
        setHasWarranty(false)
        setPhoto(null)
      }, 3000)
    } catch (err) {
      setSubmitError(err instanceof Error ? err.message : t('common:productIntake.submitError'))
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <Box sx={{ maxWidth: 800, mx: 'auto', p: { xs: 2, sm: 4 } }}>
      <Box sx={{ mb: 4 }}>
        <Typography variant="h4" gutterBottom>{t('productIntake.title')}</Typography>
        <Typography color="text.secondary">{t('productIntake.subtitle')}</Typography>
      </Box>

      {success && (
        <Alert icon={<CheckCircleIcon fontSize="inherit" />} severity="success" sx={{ mb: 3 }}>
          {t('common:productIntake.successMessage')}
        </Alert>
      )}

      {submitError && (
        <Alert severity="error" sx={{ mb: 3 }} onClose={() => setSubmitError('')}>
          {submitError}
        </Alert>
      )}

      <Paper sx={{ p: { xs: 3, sm: 4 } }}>
        <form onSubmit={handleSubmit}>
          <Box sx={{ display: 'flex', flexDirection: 'column', gap: 3 }}>

            <CustomerPicker
              value={customer}
              onChange={setCustomer}
              label={t('productIntake.selectCustomer')}
              size="medium"
            />

            <Box sx={{ display: 'flex', gap: 3, flexDirection: { xs: 'column', sm: 'row' } }}>
              <TextField
                required
                label={t('productIntake.deviceModel')}
                value={deviceModel}
                onChange={(e) => setDeviceModel(e.target.value)}
                fullWidth
              />
              <TextField
                required
                label={t('productIntake.serialNumber')}
                value={serialNumber}
                onChange={(e) => setSerialNumber(e.target.value)}
                fullWidth
              />
            </Box>

            <TextField
              required
              label={t('productIntake.complaint')}
              multiline
              rows={4}
              value={complaint}
              onChange={(e) => setComplaint(e.target.value)}
              fullWidth
            />

            <Box sx={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', flexWrap: 'wrap', gap: 2 }}>
              <FormControlLabel
                control={<Switch checked={hasWarranty} onChange={(e) => setHasWarranty(e.target.checked)} color="primary" />}
                label={t('productIntake.hasWarranty')}
              />
              <Button variant="outlined" component="label" startIcon={<PhotoCameraIcon />}>
                {photo ? photo.name : t('productIntake.uploadPhoto')}
                <input type="file" hidden accept="image/*" onChange={(e) => setPhoto(e.target.files ? e.target.files[0] : null)} />
              </Button>
            </Box>

            <Box sx={{ mt: 2, display: 'flex', justifyContent: 'flex-end' }}>
              <Button
                type="submit"
                variant="contained"
                size="large"
                disabled={submitting || !customer || !deviceModel.trim() || !serialNumber.trim() || !complaint.trim()}
              >
                {submitting
                  ? <><CircularProgress size={20} sx={{ color: 'white', mr: 1 }} />{t('common:productIntake.submitting')}</>
                  : t('productIntake.submit')}
              </Button>
            </Box>

          </Box>
        </form>
      </Paper>
    </Box>
  )
}

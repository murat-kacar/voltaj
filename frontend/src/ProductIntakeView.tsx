import { useState, useEffect } from 'react'
import {
  Box,
  Typography,
  Paper,
  TextField,
  Button,
  FormControlLabel,
  Switch,
  MenuItem,
  CircularProgress,
  Alert
} from '@mui/material'
import PhotoCameraIcon from '@mui/icons-material/PhotoCamera'
import CheckCircleIcon from '@mui/icons-material/CheckCircle'
import { useI18n } from './i18n'
import { customersApi, type Customer } from './api'

export function ProductIntakeView() {
  const { translate: t } = useI18n()
  
  const [customers, setCustomers] = useState<Customer[]>([])
  const [loading, setLoading] = useState(true)
  const [submitting, setSubmitting] = useState(false)
  const [success, setSuccess] = useState(false)
  
  // Form State
  const [customerId, setCustomerId] = useState('')
  const [deviceModel, setDeviceModel] = useState('')
  const [serialNumber, setSerialNumber] = useState('')
  const [complaint, setComplaint] = useState('')
  const [hasWarranty, setHasWarranty] = useState(false)
  const [photo, setPhoto] = useState<File | null>(null)

  useEffect(() => {
    customersApi.list()
      .then(setCustomers)
      .finally(() => setLoading(false))
  }, [])

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault()
    setSubmitting(true)
    
    // Simulate API call
    setTimeout(() => {
      setSubmitting(false)
      setSuccess(true)
      
      // Reset form after a brief delay
      setTimeout(() => {
        setSuccess(false)
        setCustomerId('')
        setDeviceModel('')
        setSerialNumber('')
        setComplaint('')
        setHasWarranty(false)
        setPhoto(null)
      }, 3000)
    }, 1500)
  }

  if (loading) {
    return <Box sx={{ p: 4, textAlign: 'center' }}><CircularProgress /></Box>
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

      <Paper sx={{ p: { xs: 3, sm: 4 } }}>
        <form onSubmit={handleSubmit}>
          <Box sx={{ display: 'flex', flexDirection: 'column', gap: 3 }}>
            
            <TextField
              select
              required
              label={t('productIntake.selectCustomer')}
              value={customerId}
              onChange={(e) => setCustomerId(e.target.value)}
              fullWidth
            >
              {customers.map((c) => (
                <MenuItem key={c.id} value={c.id}>{c.fullName}</MenuItem>
              ))}
            </TextField>

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
              
              <Button
                variant="outlined"
                component="label"
                startIcon={<PhotoCameraIcon />}
              >
                {photo ? photo.name : t('productIntake.uploadPhoto')}
                <input
                  type="file"
                  hidden
                  accept="image/*"
                  onChange={(e) => setPhoto(e.target.files ? e.target.files[0] : null)}
                />
              </Button>
            </Box>

            <Box sx={{ mt: 2, display: 'flex', justifyContent: 'flex-end' }}>
              <Button 
                type="submit" 
                variant="contained" 
                size="large"
                disabled={submitting}
              >
                {submitting ? <CircularProgress size={24} sx={{ color: 'white' }} /> : t('productIntake.submit')}
              </Button>
            </Box>

          </Box>
        </form>
      </Paper>
    </Box>
  )
}

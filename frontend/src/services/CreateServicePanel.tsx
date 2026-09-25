import { useState, useEffect } from 'react'
import { Typography, Button, TextField, Stack, Paper, CircularProgress, MenuItem, FormControlLabel, Switch, Box, Dialog, DialogTitle, DialogContent, DialogActions } from '@mui/material'
import { useI18n } from '../i18n'
import { servicesApi } from '../api/services'
import { customersApi, type Customer } from '../api/customers'

export function CreateServicePanel({ onCreated, onCancel }: { onCreated: (id: string) => void, onCancel: () => void }) {
  const { translate: t } = useI18n()
  
  const [customerId, setCustomerId] = useState('')
  const [title, setTitle] = useState('')
  const [notes, setNotes] = useState('')
  const [saving, setSaving] = useState(false)

  const [isMaintenanceContract, setIsMaintenanceContract] = useState(false)
  const [maintenancePeriod, setMaintenancePeriod] = useState<'Monthly'|'Quarterly'|'Biannually'|'Yearly'|''>('')
  const [nextMaintenanceDate, setNextMaintenanceDate] = useState('')

  const [customers, setCustomers] = useState<Customer[]>([])
  
  // New Customer State
  const [customerModalOpen, setCustomerModalOpen] = useState(false)
  const [newCustomerName, setNewCustomerName] = useState('')
  const [newCustomerPhone, setNewCustomerPhone] = useState('')
  const [creatingCustomer, setCreatingCustomer] = useState(false)

  // eslint-disable-next-line react-hooks/exhaustive-deps
  useEffect(() => {
    customersApi.list().then(setCustomers)
  }, [])

  const handleSave = async () => {
    if (!customerId || !title) return
    setSaving(true)
    try {
      const result = await servicesApi.createDraft({ 
        customerId, 
        title, 
        notes,
        isMaintenanceContract,
        maintenancePeriod: isMaintenanceContract ? (maintenancePeriod || null) : null,
        nextMaintenanceDate: isMaintenanceContract ? (nextMaintenanceDate || null) : null
      })
      onCreated(result.id)
    } catch (e) {
      console.error(e)
    } finally {
      setSaving(false)
    }
  }

  const handleCreateCustomer = async () => {
    if (!newCustomerName || !newCustomerPhone) return
    setCreatingCustomer(true)
    try {
      const c = await customersApi.create({ fullName: newCustomerName, phone: newCustomerPhone })
      setCustomers(prev => [...prev, c])
      setCustomerId(c.id)
      setCustomerModalOpen(false)
      setNewCustomerName('')
      setNewCustomerPhone('')
    } catch (e) {
      console.error(e)
      alert('Error creating customer')
    } finally {
      setCreatingCustomer(false)
    }
  }

  return (
    <Paper sx={{ p: 4, borderRadius: 2 }}>
      <Typography variant="h5" sx={{ fontWeight: 'bold', mb: 1 }}>{t('services:detail.createTitle')}</Typography>
      <Typography variant="body2" color="text.secondary" sx={{ mb: 4 }}>{t('services:detail.createDesc')}</Typography>

      <Stack spacing={3}>
        <Box sx={{ display: 'flex', gap: 1, alignItems: 'flex-start' }}>
          <TextField
            select
            label={t('services:detail.customer')}
            value={customerId}
            onChange={e => setCustomerId(e.target.value)}
            fullWidth
            required
          >
            {customers.map(c => (
              <MenuItem key={c.id} value={c.id}>{c.fullName}</MenuItem>
            ))}
          </TextField>
          <Button 
            variant="outlined" 
            onClick={() => setCustomerModalOpen(true)}
            sx={{ minWidth: 140, height: 56 }}
          >
            + Yeni Müşteri
          </Button>
        </Box>

        <TextField
          label={t('services:columns.title')}
          value={title}
          onChange={e => setTitle(e.target.value)}
          fullWidth
          required
        />

        <TextField
          label={t('services:detail.notes')}
          value={notes}
          onChange={e => setNotes(e.target.value)}
          multiline
          rows={4}
          fullWidth
        />

        <Box sx={{ bgcolor: 'action.hover', p: 2, borderRadius: 1 }}>
          <FormControlLabel
            control={<Switch checked={isMaintenanceContract} onChange={e => setIsMaintenanceContract(e.target.checked)} />}
            label={t('services:detail.isMaintenanceContract', 'Bu bir Bakım Sözleşmesi (Periyodik Hizmet)')}
          />

          {isMaintenanceContract && (
            <Stack direction="row" spacing={2} sx={{ mt: 2 }}>
              <TextField
                select
                label={t('services:detail.maintenancePeriod', 'Periyot')}
                value={maintenancePeriod}
                onChange={e => setMaintenancePeriod(e.target.value as any)}
                fullWidth
                size="small"
              >
                <MenuItem value="Monthly">Aylık</MenuItem>
                <MenuItem value="Quarterly">3 Aylık</MenuItem>
                <MenuItem value="Biannually">6 Aylık</MenuItem>
                <MenuItem value="Yearly">Yıllık</MenuItem>
              </TextField>
              <TextField
                type="date"
                label={t('services:detail.nextMaintenanceDate', 'Sonraki Bakım Tarihi')}
                value={nextMaintenanceDate}
                onChange={e => setNextMaintenanceDate(e.target.value)}
                fullWidth
                size="small"
                InputLabelProps={{ shrink: true }}
              />
            </Stack>
          )}
        </Box>

        <Stack direction="row" spacing={2} sx={{ justifyContent: 'flex-end', pt: 2 }}>
          <Button onClick={onCancel} disabled={saving}>{t('services:detail.cancel')}</Button>
          <Button variant="contained" onClick={handleSave} disabled={saving || !customerId || !title}>
            {saving ? <CircularProgress size={24} /> : t('services:detail.saveDraft')}
          </Button>
        </Stack>
      </Stack>

      {/* Quick Add Customer Dialog */}
      <Dialog open={customerModalOpen} onClose={() => setCustomerModalOpen(false)} maxWidth="xs" fullWidth>
        <DialogTitle sx={{ fontWeight: 'bold' }}>Yeni Müşteri Ekle</DialogTitle>
        <DialogContent>
          <Stack spacing={2} sx={{ mt: 1 }}>
            <TextField 
              label="Müşteri Adı Soyadı / Unvan" 
              value={newCustomerName} 
              onChange={e => setNewCustomerName(e.target.value)} 
              fullWidth 
              required 
            />
            <TextField 
              label="Telefon" 
              value={newCustomerPhone} 
              onChange={e => setNewCustomerPhone(e.target.value)} 
              fullWidth 
              required 
            />
          </Stack>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setCustomerModalOpen(false)}>İptal</Button>
          <Button 
            variant="contained" 
            onClick={handleCreateCustomer} 
            disabled={creatingCustomer || !newCustomerName || !newCustomerPhone}
          >
            {creatingCustomer ? <CircularProgress size={24} /> : 'Ekle'}
          </Button>
        </DialogActions>
      </Dialog>
    </Paper>
  )
}

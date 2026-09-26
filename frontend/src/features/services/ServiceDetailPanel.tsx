import { useState, useEffect } from 'react'
import { Box, Typography, Paper, CircularProgress, Stack, Chip, Tabs, Tab, Divider, Button, Menu, MenuItem, IconButton, Select, FormControl, InputLabel, Dialog, DialogTitle, DialogContent, DialogActions } from '@mui/material'
import MoreVertIcon from '@mui/icons-material/MoreVert'
import PrintIcon from '@mui/icons-material/Print'
import ArrowForwardIcon from '@mui/icons-material/ArrowForward'
import { useI18n } from '../../i18n'
import { servicesApi, type ServiceDto, type ServiceSubStatus } from '../../api/services'
import { PrintPreviewDialog, type PrintDocumentType, type PrintData } from '../../components/print/PrintPreviewDialog'

export function ServiceDetailPanel({ serviceId, onUpdated }: { serviceId: string, onUpdated: () => void }) {
  const { translate: t } = useI18n()
  const [tab, setTab] = useState(0)

  const [service, setService] = useState<ServiceDto | null>(null)
  const [loading, setLoading] = useState(false)
  const [actionLoading, setActionLoading] = useState(false)

  const [anchorEl, setAnchorEl] = useState<null | HTMLElement>(null)
  const [printAnchorEl, setPrintAnchorEl] = useState<null | HTMLElement>(null)

  const [subStatusDialogOpen, setSubStatusDialogOpen] = useState(false)
  const [selectedSubStatus, setSelectedSubStatus] = useState<ServiceSubStatus>('None')

  const [printDocType, setPrintDocType] = useState<PrintDocumentType | null>(null)

  const reload = () => {
    setLoading(true)
    servicesApi.get(serviceId).then(setService).finally(() => setLoading(false))
  }

  // eslint-disable-next-line react-hooks/exhaustive-deps
  useEffect(() => { reload() }, [serviceId])

  const fmtDate = (d: string) => new Date(d).toLocaleDateString()
  const fmtCurrency = (a: number) => new Intl.NumberFormat('en-US', { style: 'currency', currency: 'USD' }).format(a)

  if (loading || !service) {
    return <Box sx={{ p: 4, textAlign: 'center' }}><CircularProgress /></Box>
  }

  const handleAction = async (actionFn: () => Promise<any>) => {
    setActionLoading(true)
    setAnchorEl(null)
    try {
      await actionFn()
      onUpdated()
      reload()
    } catch (e) {
      console.error(e)
    } finally {
      setActionLoading(false)
    }
  }

  const handleUpdateSubStatus = () => {
    handleAction(() => servicesApi.updateSubStatus(service.id, selectedSubStatus))
    setSubStatusDialogOpen(false)
  }

  const handlePrint = (type: 'quote' | 'revise' | 'invoice') => {
    setPrintAnchorEl(null)
    const mapType = {
      'quote': 'Quote',
      'revise': 'Revision',
      'invoice': 'Invoice'
    } as const
    setPrintDocType(mapType[type] || 'Quote')
  }

  const buildPrintData = (): PrintData | null => {
    if (!service) return null
    return {
      documentNo: service.number,
      date: fmtDate(service.createdAt),
      dueDate: service.validUntil ? fmtDate(service.validUntil) : undefined,
      customerName: `Müşteri ID: ${service.customerId}`, // To be resolved with real customer name from DB join
      customerDetails: 'Kayıtlı Müşteri\nAdresi Sistemde Bulunmaktadır.',
      items: service.items.map(i => ({
        description: i.description,
        quantity: i.quantity,
        unit: i.unit,
        unitPrice: i.unitPrice,
        lineTotal: i.lineTotal
      })),
      subTotal: service.billing.currentTotal,
      taxTotal: service.billing.currentTotal * 0.2, // mock tax computation
      grandTotal: service.billing.currentTotal * 1.2,
      notes: service.notes || undefined
    }
  }

  return (
    <Box>
      <Paper sx={{ p: 3, borderRadius: 2, mb: 3 }}>
        <Stack direction="row" spacing={2} sx={{ justifyContent: 'space-between', alignItems: 'flex-start', mb: 2 }}>
          <Box>
            <Typography variant="h5" gutterBottom sx={{ fontWeight: 'bold' }}>{service.title || service.number}</Typography>
            <Typography variant="body2" color="text.secondary" sx={{ mb: 1 }}>#{service.number} • {fmtDate(service.createdAt)}</Typography>
            
            {service.isMaintenanceContract && (
              <Box sx={{ display: 'flex', gap: 1, alignItems: 'center' }}>
                <Chip size="small" color="secondary" label={`Bakım Sözleşmesi (${service.maintenancePeriod === 'Monthly' ? 'Aylık' : service.maintenancePeriod === 'Quarterly' ? '3 Aylık' : service.maintenancePeriod === 'Biannually' ? '6 Aylık' : 'Yıllık'})`} sx={{ fontWeight: 'bold' }} />
                {service.nextMaintenanceDate && (
                  <Chip size="small" variant="outlined" color="secondary" label={`Sonraki Bakım: ${fmtDate(service.nextMaintenanceDate)}`} />
                )}
              </Box>
            )}
          </Box>
          <Stack direction="row" spacing={1}>
            {service.status === 'Draft' && (
              <Button 
                variant="contained" 
                color="primary"
                onClick={() => handleAction(() => servicesApi.issue(service.id))}
                disabled={actionLoading}
                endIcon={<ArrowForwardIcon />}
              >
                Send Quote
              </Button>
            )}
            {service.status === 'Issued' && (
              <Button 
                variant="contained" 
                color="success"
                onClick={() => handleAction(() => servicesApi.accept(service.id))}
                disabled={actionLoading}
              >
                Accept
              </Button>
            )}
            {['Accepted', 'InProgress'].includes(service.status) && (
              <Button 
                variant="contained" 
                color="success"
                onClick={() => handleAction(() => servicesApi.complete(service.id))}
                disabled={actionLoading}
              >
                Complete
              </Button>
            )}

            <Button
              variant="outlined"
              startIcon={<PrintIcon />}
              onClick={(e) => setPrintAnchorEl(e.currentTarget)}
            >
              Print
            </Button>
            <Menu anchorEl={printAnchorEl} open={Boolean(printAnchorEl)} onClose={() => setPrintAnchorEl(null)}>
              <MenuItem onClick={() => handlePrint('quote')}>Quote (Teklif)</MenuItem>
              <MenuItem onClick={() => handlePrint('revise')}>Revised Quote (Revize)</MenuItem>
              <MenuItem onClick={() => handlePrint('invoice')}>Invoice (Fatura)</MenuItem>
            </Menu>

            <IconButton onClick={(e) => setAnchorEl(e.currentTarget)}>
              <MoreVertIcon />
            </IconButton>
            <Menu anchorEl={anchorEl} open={Boolean(anchorEl)} onClose={() => setAnchorEl(null)}>
              <MenuItem onClick={() => { setAnchorEl(null); setSelectedSubStatus(service.subStatus); setSubStatusDialogOpen(true) }}>
                Change Sub-Status
              </MenuItem>
              <Divider />
              {['Draft', 'Issued'].includes(service.status) && (
                <MenuItem onClick={() => handleAction(() => servicesApi.reject(service.id, 'Customer rejected'))} sx={{ color: 'error.main' }}>Reject</MenuItem>
              )}
              {['Accepted', 'InProgress'].includes(service.status) && (
                <MenuItem onClick={() => handleAction(() => servicesApi.hold(service.id, 'Waiting'))}>Put on Hold</MenuItem>
              )}
              {service.status === 'OnHold' && (
                <MenuItem onClick={() => handleAction(() => servicesApi.resume(service.id))}>Resume</MenuItem>
              )}
              <MenuItem onClick={() => handleAction(() => servicesApi.cancel(service.id, 'Manual cancel'))} sx={{ color: 'error.main' }}>Cancel</MenuItem>
            </Menu>
          </Stack>
        </Stack>

        <Stack direction="row" spacing={1} sx={{ mb: 2 }}>
          <Chip label={t(`services:status.${service.status}` as any) || service.status} color="primary" />
          {service.subStatus && service.subStatus !== 'None' && (
            <Chip label={t(`services:subStatus.${service.subStatus}` as any) || service.subStatus} variant="outlined" />
          )}
        </Stack>

        <Divider sx={{ my: 2 }} />

        <Stack direction="row" spacing={4}>
          <Box>
            <Typography variant="caption" color="text.secondary" sx={{ display: 'block' }}>{t('services:detail.customer')}</Typography>
            <Typography variant="body1">{service.customerId}</Typography>
          </Box>
          <Box>
            <Typography variant="caption" color="text.secondary" sx={{ display: 'block' }}>{t('services:columns.total')}</Typography>
            <Typography variant="body1" sx={{ fontWeight: 'bold' }}>{fmtCurrency(service.billing.currentTotal)}</Typography>
          </Box>
        </Stack>
      </Paper>

      <Paper sx={{ borderRadius: 2, overflow: 'hidden' }}>
        <Tabs value={tab} onChange={(_, v) => setTab(v)} variant="fullWidth">
          <Tab label={t('services:detail.overview')} />
          <Tab label={t('services:detail.items')} />
          <Tab label={t('services:detail.history')} />
        </Tabs>
        <Divider />
        <Box sx={{ p: 3 }}>
          {tab === 0 && (
            <Box>
              <Typography variant="subtitle2" gutterBottom>{t('services:detail.notes')}</Typography>
              <Typography variant="body2" color="text.secondary">{service.notes || '-'}</Typography>
            </Box>
          )}
          {tab === 1 && (
            <Box>
              <Typography variant="subtitle2" gutterBottom>Quote Lines</Typography>
              {service.items.length === 0 ? (
                <Typography variant="body2" color="text.secondary">No items added yet.</Typography>
              ) : (
                <Box>
                  {service.items.map(i => (
                    <Stack key={i.id} direction="row" spacing={2} sx={{ justifyContent: 'space-between', py: 1, borderBottom: 1, borderColor: 'divider' }}>
                      <Typography>{i.description}</Typography>
                      <Typography>{fmtCurrency(i.lineTotal)}</Typography>
                    </Stack>
                  ))}
                </Box>
              )}
            </Box>
          )}
          {tab === 2 && (
            <Box>
              <Typography variant="body2" color="text.secondary">Audit history will be displayed here.</Typography>
            </Box>
          )}
        </Box>
      </Paper>

      <Dialog open={subStatusDialogOpen} onClose={() => setSubStatusDialogOpen(false)} maxWidth="xs" fullWidth>
        <DialogTitle>Update Sub-Status</DialogTitle>
        <DialogContent>
          <FormControl fullWidth sx={{ mt: 1 }}>
            <InputLabel>Sub-Status</InputLabel>
            <Select
              value={selectedSubStatus}
              label="Sub-Status"
              onChange={(e) => setSelectedSubStatus(e.target.value as ServiceSubStatus)}
            >
              <MenuItem value="None">{t('services:subStatus.None')}</MenuItem>
              <MenuItem value="WaitingForInternalApproval">{t('services:subStatus.WaitingForInternalApproval')}</MenuItem>
              <MenuItem value="AwaitingCustomerResponse">{t('services:subStatus.AwaitingCustomerResponse')}</MenuItem>
              <MenuItem value="InRevision">{t('services:subStatus.InRevision')}</MenuItem>
              <MenuItem value="WaitingForMaterials">{t('services:subStatus.WaitingForMaterials')}</MenuItem>
              <MenuItem value="Scheduled">{t('services:subStatus.Scheduled')}</MenuItem>
              <MenuItem value="InTransit">{t('services:subStatus.InTransit')}</MenuItem>
              <MenuItem value="OnSite">{t('services:subStatus.OnSite')}</MenuItem>
              <MenuItem value="WorkInProgress">{t('services:subStatus.WorkInProgress')}</MenuItem>
              <MenuItem value="QualityCheck">{t('services:subStatus.QualityCheck')}</MenuItem>
              <MenuItem value="PendingCustomerApproval">{t('services:subStatus.PendingCustomerApproval')}</MenuItem>
              <MenuItem value="WaitingForAccess">{t('services:subStatus.WaitingForAccess')}</MenuItem>
              <MenuItem value="WaitingForPayment">{t('services:subStatus.WaitingForPayment')}</MenuItem>
              <MenuItem value="WeatherDelay">{t('services:subStatus.WeatherDelay')}</MenuItem>
              <MenuItem value="Invoiced">{t('services:subStatus.Invoiced')}</MenuItem>
              <MenuItem value="Paid">{t('services:subStatus.Paid')}</MenuItem>
            </Select>
          </FormControl>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setSubStatusDialogOpen(false)}>{t('services:detail.cancel')}</Button>
          <Button onClick={handleUpdateSubStatus} variant="contained" disabled={actionLoading}>Update</Button>
        </DialogActions>
      </Dialog>

      <PrintPreviewDialog 
        open={printDocType !== null} 
        onClose={() => setPrintDocType(null)} 
        type={printDocType || 'Quote'} 
        data={buildPrintData()} 
      />
    </Box>
  )
}

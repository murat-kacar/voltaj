import { useCallback, useEffect, useState } from 'react'
import {
  Alert, Box, Button, Chip, CircularProgress, Divider, Drawer,
  IconButton, List, ListItem, ListItemText, Stack, TextField, Typography,
} from '@mui/material'
import CloseIcon from '@mui/icons-material/Close'
import CheckCircleIcon from '@mui/icons-material/CheckCircle'
import RadioButtonUncheckedIcon from '@mui/icons-material/RadioButtonUnchecked'
import { workOrdersApi, customersApi, type WorkOrder, type Customer } from './api'
import { useI18n } from './i18n'

function statusColor(status: string): 'default' | 'primary' | 'secondary' | 'error' | 'info' | 'success' | 'warning' {
  switch (status) {
    case 'InProgress': return 'warning'
    case 'Completed': return 'success'
    case 'ReadyForBilling': return 'info'
    case 'Invoiced': return 'success'
    case 'Cancelled': return 'error'
    case 'NoShow': return 'error'
    case 'OnHold': return 'warning'
    case 'Assigned': return 'primary'
    case 'EnRoute': return 'primary'
    default: return 'default'
  }
}

function currentUserId(): string | null {
  try {
    const s = localStorage.getItem('voltflow.session')
    return s ? (JSON.parse(s) as { userId: string }).userId : null
  } catch { return null }
}

type ReasonTarget = 'cancel' | 'hold' | 'noShow'

export function WorkOrderDetailDrawer({
  order: initialOrder,
  onClose,
  onUpdated,
}: {
  order: WorkOrder
  onClose: () => void
  onUpdated?: (updated: WorkOrder) => void
}) {
  const { translate: t } = useI18n()
  const [order, setOrder] = useState<WorkOrder>(initialOrder)
  const [customer, setCustomer] = useState<Customer | null>(null)
  const [busy, setBusy] = useState(false)
  const [actionError, setActionError] = useState('')

  const [showReasonFor, setShowReasonFor] = useState<ReasonTarget | null>(null)
  const [reason, setReason] = useState('')
  const [showCompleteForm, setShowCompleteForm] = useState(false)
  const [signature, setSignature] = useState('')
  const [photoUrl, setPhotoUrl] = useState('')

  const [showAddMaterial, setShowAddMaterial] = useState(false)
  const [matDesc, setMatDesc] = useState('')
  const [matQty, setMatQty] = useState('1')
  const [matPrice, setMatPrice] = useState('')

  useEffect(() => {
    let ignore = false
    customersApi.get(order.customerId).then((c) => { if (!ignore) setCustomer(c) }).catch(() => {})
    return () => { ignore = true }
  }, [order.customerId])

  const act = useCallback(
    async (fn: () => Promise<WorkOrder>) => {
      setBusy(true)
      setActionError('')
      try {
        const updated = await fn()
        setOrder(updated)
        onUpdated?.(updated)
        setReason('')
        setShowReasonFor(null)
        setShowCompleteForm(false)
        setSignature('')
        setPhotoUrl('')
        setShowAddMaterial(false)
        setMatDesc('')
        setMatQty('1')
        setMatPrice('')
      } catch (err) {
        setActionError(err instanceof Error ? err.message : t('workOrders:errors.actionFailed'))
      } finally {
        setBusy(false)
      }
    },
    [onUpdated, t],
  )

  const activeCheckIn = order.timeEntries?.find((e) => !e.checkOutTime)
  const s = order.status
  const isClosed = s === 'Invoiced' || s === 'Cancelled' || s === 'NoShow'
  const userId = currentUserId()

  const reasonLabel = showReasonFor === 'cancel' ? 'Cancellation reason' : showReasonFor === 'hold' ? 'Hold reason' : 'No-show reason'

  return (
    <Drawer
      anchor="right"
      open={true}
      onClose={onClose}
      className="drawer open"
      sx={{ '& .MuiDrawer-paper': { width: { xs: '100%', sm: 480 } } }}
    >
      <Box sx={{ p: 2, display: 'flex', alignItems: 'center', justifyContent: 'space-between' }}>
        <Typography variant="h6">{t('workOrders:drawer.title')}</Typography>
        <IconButton onClick={onClose}><CloseIcon /></IconButton>
      </Box>
      <Divider />

      <Box sx={{ p: 2, overflowY: 'auto', flex: 1 }}>
        {/* Header */}
        <Typography variant="overline" color="text.secondary">{t('workOrders:drawer.orderId')}</Typography>
        <Typography variant="h5" gutterBottom>{order.number}</Typography>
        <Typography variant="body1" gutterBottom>{order.title}</Typography>

        <Stack direction="row" spacing={1} sx={{ mb: 2, flexWrap: 'wrap' }}>
          <Chip label={order.status} color={statusColor(order.status)} size="small" />
          <Chip
            icon={order.isSafetyChecklistCompleted ? <CheckCircleIcon /> : <RadioButtonUncheckedIcon />}
            label="Safety"
            color={order.isSafetyChecklistCompleted ? 'success' : 'default'}
            size="small"
            variant={order.isSafetyChecklistCompleted ? 'filled' : 'outlined'}
          />
        </Stack>

        <Box sx={{ mb: 1 }}>
          <Typography variant="caption" color="text.secondary" sx={{ display: 'block' }}>{t('workOrders:drawer.customerId')}</Typography>
          <Typography variant="body2">{customer?.fullName ?? order.customerId}</Typography>
        </Box>

        <Box sx={{ mb: 2 }}>
          <Typography variant="caption" color="text.secondary" sx={{ display: 'block' }}>{t('workOrders:drawer.assignedTo')}</Typography>
          <Typography variant="body2">{order.assignedUserId ? order.assignedUserId : t('workOrders:drawer.unassigned')}</Typography>
        </Box>

        {order.holdReason && (
          <Alert severity="warning" sx={{ mb: 2 }}>Hold: {order.holdReason}</Alert>
        )}

        {/* Actions */}
        {!isClosed && (
          <>
            <Divider sx={{ my: 2 }} />
            <Typography variant="overline" color="text.secondary">{t('workOrders:drawer.actions')}</Typography>

            {actionError && <Alert severity="error" sx={{ mt: 1, mb: 1 }}>{actionError}</Alert>}
            {busy && <Box sx={{ display: 'flex', justifyContent: 'center', my: 1 }}><CircularProgress size={22} /></Box>}

            <Stack spacing={1} sx={{ mt: 1 }}>
              {/* Open: assign to me */}
              {s === 'Open' && userId && (
                <Button variant="outlined" size="small" disabled={busy} data-testid="03101-assign-btn"
                  onClick={() => act(() => workOrdersApi.assign(order.id, userId))}>
                  {t('workOrders:drawer.assignMe')}
                </Button>
              )}

              {/* Assigned: en-route */}
              {s === 'Assigned' && (
                <Button variant="outlined" size="small" disabled={busy} data-testid="03101-enroute-btn"
                  onClick={() => act(() => workOrdersApi.enRoute(order.id))}>
                  {t('workOrders:drawer.enRoute')}
                </Button>
              )}

              {/* Assigned / EnRoute: safety checklist */}
              {(s === 'Assigned' || s === 'EnRoute') && !order.isSafetyChecklistCompleted && (
                <Button variant="outlined" size="small" color="warning" disabled={busy} data-testid="03201-safety-btn"
                  onClick={() => act(() => workOrdersApi.safetyChecklist(order.id))}>
                  Complete Safety Checklist
                </Button>
              )}

              {/* Assigned / EnRoute: start (only after safety) */}
              {(s === 'Assigned' || s === 'EnRoute') && order.isSafetyChecklistCompleted && (
                <Button variant="contained" size="small" disabled={busy} data-testid="03201-start-btn"
                  onClick={() => act(() => workOrdersApi.start(order.id))}>
                  Start Work Order
                </Button>
              )}

              {/* InProgress: check-in / check-out */}
              {s === 'InProgress' && !activeCheckIn && (
                <Button variant="contained" size="small" disabled={busy} data-testid="03201-checkin-btn"
                  onClick={() => act(() => workOrdersApi.checkIn(order.id))}>
                  {t('workOrders:drawer.checkIn')}
                </Button>
              )}
              {s === 'InProgress' && activeCheckIn && (
                <Button variant="outlined" size="small" disabled={busy} data-testid="03201-checkout-btn"
                  onClick={() => act(() => workOrdersApi.checkOut(order.id))}>
                  {t('workOrders:drawer.checkOut')}
                </Button>
              )}

              {/* InProgress: complete */}
              {s === 'InProgress' && !showCompleteForm && (
                <Button variant="contained" size="small" color="success" disabled={busy} data-testid="03401-complete-btn"
                  onClick={() => setShowCompleteForm(true)}>
                  {t('workOrders:drawer.completeJob')}
                </Button>
              )}
              {s === 'InProgress' && showCompleteForm && (
                <Box sx={{ border: 1, borderColor: 'divider', borderRadius: 1, p: 1.5 }}>
                  <TextField fullWidth size="small" label={t('workOrders:drawer.signature')} value={signature}
                    onChange={(e) => setSignature(e.target.value)} sx={{ mb: 1 }} />
                  <TextField fullWidth size="small" label={t('workOrders:drawer.photoUrl')} value={photoUrl}
                    onChange={(e) => setPhotoUrl(e.target.value)} sx={{ mb: 1 }} />
                  <Stack direction="row" spacing={1}>
                    <Button variant="contained" size="small" color="success" data-testid="03401-complete-submit-btn"
                      disabled={busy || (!signature.trim() && !photoUrl.trim())}
                      onClick={() => act(() => workOrdersApi.complete(order.id, signature || undefined, photoUrl || undefined))}>
                      Confirm Complete
                    </Button>
                    <Button size="small" onClick={() => setShowCompleteForm(false)}>Cancel</Button>
                  </Stack>
                </Box>
              )}

              {/* InProgress: add material */}
              {s === 'InProgress' && !showAddMaterial && (
                <Button variant="outlined" size="small" disabled={busy}
                  onClick={() => setShowAddMaterial(true)}>
                  {t('workOrders:drawer.addMaterial')}
                </Button>
              )}
              {s === 'InProgress' && showAddMaterial && (
                <Box sx={{ border: 1, borderColor: 'divider', borderRadius: 1, p: 1.5 }}>
                  <TextField fullWidth size="small" label={t('workOrders:drawer.materialDesc')} value={matDesc}
                    onChange={(e) => setMatDesc(e.target.value)} sx={{ mb: 1 }} />
                  <Stack direction="row" spacing={1} sx={{ mb: 1 }}>
                    <TextField size="small" label={t('workOrders:drawer.quantity')} type="number" value={matQty}
                      onChange={(e) => setMatQty(e.target.value)} sx={{ width: 100 }} />
                    <TextField size="small" label={t('workOrders:drawer.unitPrice')} type="number" value={matPrice}
                      onChange={(e) => setMatPrice(e.target.value)} sx={{ flex: 1 }} />
                  </Stack>
                  <Stack direction="row" spacing={1}>
                    <Button variant="contained" size="small" disabled={busy || !matDesc || !matQty || !matPrice}
                      onClick={() => act(() => workOrdersApi.addItem(order.id, matDesc, Number(matQty), Number(matPrice)))}>
                      {t('workOrders:drawer.saveMaterial')}
                    </Button>
                    <Button size="small" onClick={() => setShowAddMaterial(false)}>Cancel</Button>
                  </Stack>
                </Box>
              )}

              {/* OnHold: resume */}
              {s === 'OnHold' && (
                <Button variant="contained" size="small" disabled={busy} data-testid="03301-resume-btn"
                  onClick={() => act(() => workOrdersApi.resume(order.id))}>
                  {t('workOrders:drawer.resumeOrder')}
                </Button>
              )}

              {/* Completed: approve for billing */}
              {s === 'Completed' && (
                <Button variant="contained" size="small" data-testid="03501-approve-billing-btn" disabled={busy}
                  onClick={() => act(() => workOrdersApi.approveBilling(order.id))}>
                  Approve for Billing
                </Button>
              )}

              {/* ReadyForBilling: invoice */}
              {s === 'ReadyForBilling' && (
                <Button variant="contained" size="small" color="success" data-testid="03501-invoice-btn" disabled={busy}
                  onClick={() => act(() => workOrdersApi.invoice(order.id))}>
                  {t('workOrders:drawer.invoiceOrder')}
                </Button>
              )}

              {/* No-show (from Assigned/EnRoute) */}
              {(s === 'Assigned' || s === 'EnRoute') && !showReasonFor && (
                <Button variant="outlined" size="small" color="warning" disabled={busy} data-testid="03201-noshow-btn"
                  onClick={() => setShowReasonFor('noShow')}>
                  {t('workOrders:drawer.noShow')}
                </Button>
              )}

              {/* Hold (from InProgress) */}
              {s === 'InProgress' && !showReasonFor && (
                <Button variant="outlined" size="small" color="warning" disabled={busy} data-testid="03301-hold-btn"
                  onClick={() => setShowReasonFor('hold')}>
                  {t('workOrders:drawer.holdOrder')}
                </Button>
              )}

              {/* Cancel (from any non-terminal state) */}
              {s !== 'Completed' && s !== 'ReadyForBilling' && !showReasonFor && (
                <Button variant="outlined" size="small" color="error" disabled={busy} data-testid="03501-cancel-btn"
                  onClick={() => setShowReasonFor('cancel')}>
                  {t('workOrders:drawer.cancelOrder')}
                </Button>
              )}

              {/* Reason inline form */}
              {showReasonFor && (
                <Box sx={{ border: 1, borderColor: 'divider', borderRadius: 1, p: 1.5 }}>
                  <TextField fullWidth size="small" label={reasonLabel} value={reason}
                    onChange={(e) => setReason(e.target.value)} sx={{ mb: 1 }} />
                  <Stack direction="row" spacing={1}>
                    <Button variant="contained" size="small"
                      color={showReasonFor === 'cancel' ? 'error' : 'warning'}
                      disabled={busy || !reason.trim()}
                      onClick={() => {
                        if (showReasonFor === 'cancel') act(() => workOrdersApi.cancel(order.id, reason))
                        else if (showReasonFor === 'hold') act(() => workOrdersApi.hold(order.id, reason))
                        else if (showReasonFor === 'noShow') act(() => workOrdersApi.noShow(order.id, reason))
                      }}>
                      Confirm
                    </Button>
                    <Button size="small" onClick={() => { setShowReasonFor(null); setReason('') }}>Cancel</Button>
                  </Stack>
                </Box>
              )}
            </Stack>
          </>
        )}

        {/* Time entries */}
        {order.timeEntries?.length > 0 && (
          <>
            <Divider sx={{ my: 2 }} />
            <Typography variant="overline" color="text.secondary">Time Entries</Typography>
            <List dense disablePadding>
              {order.timeEntries.map((entry, i) => (
                <ListItem key={i} disablePadding>
                  <ListItemText
                    primary={`Check-in: ${new Date(entry.checkInTime).toLocaleString()}`}
                    secondary={entry.checkOutTime ? `Check-out: ${new Date(entry.checkOutTime).toLocaleString()}` : 'Active'}
                  />
                </ListItem>
              ))}
            </List>
          </>
        )}

        {/* Materials */}
        {order.items?.length > 0 && (
          <>
            <Divider sx={{ my: 2 }} />
            <Typography variant="overline" color="text.secondary">{t('workOrders:drawer.fieldMaterials')}</Typography>
            <List dense disablePadding>
              {order.items.map((item) => (
                <ListItem key={item.id} disablePadding>
                  <ListItemText
                    primary={item.description}
                    secondary={`${item.quantity} × ${item.unitPrice} = ${item.lineTotal}`}
                  />
                </ListItem>
              ))}
            </List>
          </>
        )}
      </Box>
    </Drawer>
  )
}

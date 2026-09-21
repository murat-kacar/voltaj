import { useEffect, useState } from 'react'
import { useNavigate, useParams } from 'react-router-dom'
import { Alert, Box, Button, CircularProgress } from '@mui/material'
import ArrowBackIcon from '@mui/icons-material/ArrowBack'
import { usersApi, workOrdersApi, type UserSummary, type WorkOrder } from './api'
import { WorkOrderDetailDrawer } from './WorkOrderDetailDrawer'
import { useI18n } from './i18n'

export function WorkOrderDetailPage() {
  const { id } = useParams<{ id: string }>()
  const navigate = useNavigate()
  const { translate: t } = useI18n()
  const [order, setOrder] = useState<WorkOrder | null>(null)
  const [userMap, setUserMap] = useState<Record<string, string>>({})
  const [error, setError] = useState('')

  useEffect(() => {
    if (!id) return
    Promise.all([
      workOrdersApi.getById(id),
      usersApi.page({ approved: true, limit: 200 }),
    ])
      .then(([wo, usersPage]) => {
        setOrder(wo)
        const um: Record<string, string> = {}
        usersPage.items.forEach((u: UserSummary) => { um[u.id] = u.name })
        setUserMap(um)
      })
      .catch(() => setError(t('workOrders:errors.loadOrderFailed')))
  }, [id, t])

  if (!order && !error) return <Box sx={{ display: 'flex', justifyContent: 'center', p: 6 }}><CircularProgress /></Box>
  if (error || !order) return (
    <Box sx={{ p: 3 }}>
      <Alert severity="error">{error || t('workOrders:errors.loadOrderFailed')}</Alert>
      <Button sx={{ mt: 2 }} startIcon={<ArrowBackIcon />} onClick={() => navigate('/work-orders')}>{t('workOrders:title')}</Button>
    </Box>
  )

  return (
    <WorkOrderDetailDrawer
      order={order}
      userMap={userMap}
      mode="page"
      onClose={() => navigate('/work-orders')}
      onUpdated={setOrder}
    />
  )
}

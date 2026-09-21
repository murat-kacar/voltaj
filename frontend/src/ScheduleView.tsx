import { useEffect, useMemo, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import {
  Box, Chip, CircularProgress, Stack, ToggleButton, ToggleButtonGroup, Typography,
} from '@mui/material'
import CalendarTodayIcon from '@mui/icons-material/CalendarToday'
import { customersApi, usersApi, workOrdersApi, type Customer, type UserSummary, type WorkOrder } from './api'
import { formatDay } from './common/format'
import { PageHeader } from './common/PageHeader'
import { useI18n } from './i18n'

const TERMINAL = new Set(['Invoiced', 'Cancelled', 'NoShow'])

function statusColor(status: string): 'default' | 'primary' | 'secondary' | 'error' | 'info' | 'success' | 'warning' {
  switch (status) {
    case 'Open': return 'default'
    case 'Assigned': return 'info'
    case 'EnRoute': return 'info'
    case 'InProgress': return 'primary'
    case 'OnHold': return 'warning'
    case 'Completed': return 'secondary'
    case 'ReadyForBilling': return 'success'
    case 'Invoiced': return 'success'
    case 'Cancelled': return 'error'
    case 'NoShow': return 'error'
    default: return 'default'
  }
}

type WOCardProps = { order: WorkOrder; customerName: string }

function WOCard({ order, customerName }: WOCardProps) {
  const navigate = useNavigate()
  const { lang } = useI18n()
  return (
    <Box
      onClick={() => navigate('/work-orders/' + order.id)}
      sx={{
        p: 1.5,
        border: '1px solid',
        borderColor: 'divider',
        borderRadius: 1,
        cursor: 'pointer',
        '&:hover': { bgcolor: 'action.hover' },
      }}
    >
      <Stack direction="row" spacing={1} sx={{ justifyContent: 'space-between', alignItems: 'flex-start' }}>
        <Box sx={{ minWidth: 0 }}>
          <Typography variant="body2" sx={{ fontWeight: 700 }}>{order.number}</Typography>
          <Typography variant="body2" noWrap>{order.title}</Typography>
          {customerName && <Typography variant="caption" color="text.secondary" noWrap>{customerName}</Typography>}
        </Box>
        <Stack spacing={0.5} sx={{ alignItems: 'flex-end', flexShrink: 0 }}>
          <Chip size="small" label={order.status} color={statusColor(order.status)} />
          {order.targetCompletionDate && (
            <Stack direction="row" spacing={0.25} sx={{ alignItems: 'center' }}>
              <CalendarTodayIcon sx={{ fontSize: 11, color: 'text.secondary' }} />
              <Typography variant="caption" color="text.secondary">{formatDay(order.targetCompletionDate, lang)}</Typography>
            </Stack>
          )}
        </Stack>
      </Stack>
    </Box>
  )
}

type TechColumnProps = { label: string; orders: WorkOrder[]; customerMap: Record<string, string> }

function TechSection({ label, orders, customerMap }: TechColumnProps) {
  return (
    <Box sx={{ minWidth: { xs: '100%', sm: 280 }, flex: '1 1 280px', maxWidth: { sm: 400 } }}>
      <Typography variant="subtitle2" sx={{ fontWeight: 700, mb: 1, px: 0.5 }}>{label} ({orders.length})</Typography>
      <Stack spacing={1}>
        {orders.map((o) => <WOCard key={o.id} order={o} customerName={customerMap[o.customerId] ?? ''} />)}
      </Stack>
    </Box>
  )
}

export function ScheduleView() {
  const { translate: t, lang: _lang } = useI18n()
  const [orders, setOrders] = useState<WorkOrder[]>([])
  const [userMap, setUserMap] = useState<Record<string, string>>({})
  const [customerMap, setCustomerMap] = useState<Record<string, string>>({})
  const [loading, setLoading] = useState(true)
  const [filter, setFilter] = useState<'all' | 'today'>('all')

  const todayStr = new Date().toISOString().slice(0, 10)

  useEffect(() => {
    Promise.all([
      workOrdersApi.list({ limit: 200 }),
      usersApi.page({ approved: true, limit: 200 }),
      customersApi.list(),
    ]).then(([woPage, usersPage, customers]) => {
      setOrders(woPage.items)
      const um: Record<string, string> = {}
      ;(usersPage.items as UserSummary[]).forEach((u) => { um[u.id] = u.name })
      setUserMap(um)
      const cm: Record<string, string> = {}
      ;(customers as Customer[]).forEach((c) => { cm[c.id] = c.fullName })
      setCustomerMap(cm)
    }).finally(() => setLoading(false))
  }, [])

  const visible = useMemo(() => {
    const active = orders.filter((o) => !TERMINAL.has(o.status))
    if (filter === 'today') return active.filter((o) => o.targetCompletionDate === todayStr)
    return active
  }, [orders, filter, todayStr])

  const groups = useMemo(() => {
    const byTech: Record<string, WorkOrder[]> = {}
    visible.forEach((o) => {
      const key = o.assignedUserId ?? '__unassigned__'
      if (!byTech[key]) byTech[key] = []
      byTech[key].push(o)
    })
    return byTech
  }, [visible])

  const sortedTechs = useMemo(() => {
    return Object.keys(groups).sort((a, b) => {
      if (a === '__unassigned__') return 1
      if (b === '__unassigned__') return -1
      return (userMap[a] ?? a).localeCompare(userMap[b] ?? b)
    })
  }, [groups, userMap])

  if (loading) return <Box sx={{ display: 'flex', justifyContent: 'center', p: 6 }}><CircularProgress /></Box>

  return (
    <Box>
      <PageHeader
        overline={t('common:schedule.eyebrow')}
        title={t('common:schedule.title')}
        actions={
          <ToggleButtonGroup size="small" exclusive value={filter} onChange={(_, v) => v && setFilter(v)}>
            <ToggleButton value="all">{t('common:schedule.allActive')}</ToggleButton>
            <ToggleButton value="today">{t('common:schedule.filterToday')}</ToggleButton>
          </ToggleButtonGroup>
        }
      />
      {visible.length === 0 ? (
        <Typography color="text.secondary" sx={{ p: 2 }}>{t('common:schedule.noOrders')}</Typography>
      ) : (
        <Box sx={{ display: 'flex', flexWrap: 'wrap', gap: 2 }}>
          {sortedTechs.map((techId) => (
            <TechSection
              key={techId}
              label={techId === '__unassigned__' ? t('common:schedule.unassigned') : (userMap[techId] ?? techId)}
              orders={groups[techId]}
              customerMap={customerMap}
            />
          ))}
        </Box>
      )}
    </Box>
  )
}

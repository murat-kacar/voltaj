import { useEffect, useState } from 'react'
import {
  Alert, Box, Button, Chip, Dialog, DialogActions, DialogContent, DialogTitle,
  MenuItem, Paper, TextField, Typography,
} from '@mui/material'
import AddIcon from '@mui/icons-material/Add'
import { DataGrid, type GridColDef } from '@mui/x-data-grid'
import { remindersApi, type ReminderDto } from '../../../api'
import { useI18n } from '../../../i18n'

type StateFilter = 'all' | 'Pending' | 'Completed' | 'Dismissed'

const STATE_COLOR: Record<string, 'warning' | 'success' | 'default'> = {
  Pending: 'warning',
  Completed: 'success',
  Dismissed: 'default',
}

export function RemindersView() {
  const { translate: t } = useI18n()
  const [stateFilter, setStateFilter] = useState<StateFilter>('Pending')
  const [rows, setRows] = useState<ReminderDto[]>([])
  const [loading, setLoading] = useState(true)
  const [listError, setListError] = useState('')
  const [reload, setReload] = useState(0)

  const [showCreate, setShowCreate] = useState(false)
  const [cType, setCType] = useState('')
  const [cEntityName, setCEntityName] = useState('')
  const [cEntityId, setCEntityId] = useState('')
  const [cMessage, setCMessage] = useState('')
  const [cDueAt, setCDueAt] = useState('')
  const [creating, setCreating] = useState(false)
  const [createError, setCreateError] = useState('')

  const [actionTarget, setActionTarget] = useState<{ id: string; kind: 'dismiss' | 'complete' } | null>(null)
  const [actionNote, setActionNote] = useState('')
  const [actioning, setActioning] = useState(false)
  const [actionError, setActionError] = useState('')

  useEffect(() => {
    let ignore = false
    remindersApi.list({ state: stateFilter === 'all' ? undefined : stateFilter })
      .then(page => { if (!ignore) { setRows(page.items); setLoading(false) } })
      .catch(() => { if (!ignore) { setListError(t('reminders:errors.loadFailed')); setLoading(false) } })
    return () => { ignore = true }
  }, [stateFilter, reload, t])

  const handleCreate = async () => {
    if (!cType.trim()) { setCreateError(t('reminders:validation.typeRequired')); return }
    if (!cEntityName.trim()) { setCreateError(t('reminders:validation.entityNameRequired')); return }
    if (!cEntityId.trim()) { setCreateError(t('reminders:validation.entityIdRequired')); return }
    if (!cMessage.trim()) { setCreateError(t('reminders:validation.messageRequired')); return }
    setCreating(true); setCreateError('')
    try {
      await remindersApi.create({
        type: cType.trim(), entityName: cEntityName.trim(), entityId: cEntityId.trim(),
        dueAt: cDueAt ? new Date(cDueAt).toISOString() : new Date().toISOString(),
        message: cMessage.trim(),
      })
      setShowCreate(false); setCType(''); setCEntityName(''); setCEntityId(''); setCMessage(''); setCDueAt('')
      setLoading(true); setListError('')
      setReload(r => r + 1)
    } catch (err) {
      setCreateError(err instanceof Error ? err.message : t('reminders:errors.createFailed'))
    } finally { setCreating(false) }
  }

  const handleAction = async () => {
    if (!actionTarget) return
    setActioning(true); setActionError('')
    try {
      if (actionTarget.kind === 'dismiss') await remindersApi.dismiss(actionTarget.id, actionNote || undefined)
      else await remindersApi.complete(actionTarget.id, actionNote || undefined)
      setActionTarget(null); setActionNote('')
      setLoading(true); setListError('')
      setReload(r => r + 1)
    } catch (err) {
      setActionError(err instanceof Error ? err.message : t('reminders:errors.actionFailed'))
    } finally { setActioning(false) }
  }

  const stateLabels: Record<string, string> = {
    Pending: t('reminders:state.Pending'),
    Completed: t('reminders:state.Completed'),
    Dismissed: t('reminders:state.Dismissed'),
  }

  const columns: GridColDef[] = [
    { field: 'type', headerName: t('reminders:table.type'), width: 160 },
    { field: 'entityName', headerName: t('reminders:table.entity'), width: 120 },
    { field: 'message', headerName: t('reminders:table.message'), flex: 1 },
    {
      field: 'dueAt', headerName: t('reminders:table.dueAt'), width: 160,
      valueFormatter: (v: string) => v ? new Date(v).toLocaleString() : '',
    },
    {
      field: 'state', headerName: t('reminders:table.state'), width: 110,
      renderCell: (params) => (
        <Chip
          label={stateLabels[params.value as string] ?? (params.value as string)}
          color={STATE_COLOR[params.value as string] ?? 'default'}
          size="small"
        />
      ),
    },
    { field: 'attempts', headerName: t('reminders:table.attempts'), width: 90, type: 'number' },
    {
      field: 'actions', headerName: '', width: 180, sortable: false,
      renderCell: (params) => {
        const row = params.row as ReminderDto
        if (row.state !== 'Pending') return null
        return (
          <Box sx={{ display: 'flex', gap: 0.5 }}>
            <Button size="small" color="success" onClick={() => { setActionTarget({ id: row.id, kind: 'complete' }); setActionNote('') }} data-testid="button-725119">
              {t('reminders:actions.complete')}
            </Button>
            <Button size="small" color="inherit" onClick={() => { setActionTarget({ id: row.id, kind: 'dismiss' }); setActionNote('') }} data-testid="button-06e911">
              {t('reminders:actions.dismiss')}
            </Button>
          </Box>
        )
      },
    },
  ]

  const filterOptions: { value: StateFilter; label: string }[] = [
    { value: 'Pending', label: t('reminders:filter.pending') },
    { value: 'Completed', label: t('reminders:filter.completed') },
    { value: 'Dismissed', label: t('reminders:filter.dismissed') },
    { value: 'all', label: t('reminders:filter.all') },
  ]

  return (
    <Box>
      <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 3 }}>
        <Box>
          <Typography variant="overline" color="text.secondary">{t('reminders:eyebrow')}</Typography>
          <Typography variant="h4">{t('reminders:title')}</Typography>
          <Typography variant="body2" color="text.secondary">{t('reminders:subtitle')}</Typography>
        </Box>
        <Button variant="contained" startIcon={<AddIcon />} onClick={() => setShowCreate(true)} data-testid="button-5fa7c7">
          {t('reminders:newReminder')}
        </Button>
      </Box>

      <TextField
        select
        size="small"
        label={t('reminders:filter.label')}
        value={stateFilter}
        onChange={e => { setLoading(true); setListError(''); setStateFilter(e.target.value as StateFilter) }}
        sx={{ mb: 2, minWidth: 200 }}
        data-testid="reminders-state-filter"
      >
        {filterOptions.map(option => <MenuItem key={option.value} value={option.value} data-testid="menuitem-ded6c2">{option.label}</MenuItem>)}
      </TextField>

      {listError && <Alert severity="error" sx={{ mb: 2 }}>{listError}</Alert>}

      <Paper sx={{ width: '100%', height: 500 }}>
        <DataGrid
          rows={rows}
          columns={columns}
          loading={loading}
          initialState={{ pagination: { paginationModel: { pageSize: 10 } } }}
          pageSizeOptions={[10, 25, 50]}
          localeText={{ noRowsLabel: t('reminders:empty') }}
        />
      </Paper>

      {/* Create dialog */}
      <Dialog open={showCreate} onClose={() => setShowCreate(false)} maxWidth="sm" fullWidth data-testid="dialog-477aac">
        <DialogTitle>
          <Typography variant="h6">{t('reminders:form.title')}</Typography>
          <Typography variant="body2" color="text.secondary">{t('reminders:form.subtitle')}</Typography>
        </DialogTitle>
        <DialogContent sx={{ display: 'flex', flexDirection: 'column', gap: 2, pt: '16px !important' }}>
          {createError && <Alert severity="error">{createError}</Alert>}
          <TextField label={t('reminders:form.type')} placeholder={t('reminders:form.typePlaceholder')} value={cType} onChange={e => setCType(e.target.value)} size="small" fullWidth  data-testid="textfield-8735d1" />
          <TextField label={t('reminders:form.entityName')} placeholder={t('reminders:form.entityNamePlaceholder')} value={cEntityName} onChange={e => setCEntityName(e.target.value)} size="small" fullWidth  data-testid="textfield-48a7a2" />
          <TextField label={t('reminders:form.entityId')} value={cEntityId} onChange={e => setCEntityId(e.target.value)} size="small" fullWidth  data-testid="textfield-bf25ea" />
          <TextField label={t('reminders:form.message')} value={cMessage} onChange={e => setCMessage(e.target.value)} size="small" fullWidth multiline rows={2}  data-testid="textfield-3ce5ba" />
          <TextField label={t('reminders:form.dueAt')} type="datetime-local" value={cDueAt} onChange={e => setCDueAt(e.target.value)} size="small" fullWidth slotProps={{ inputLabel: { shrink: true } }}  data-testid="textfield-e0d35d" />
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setShowCreate(false)} data-testid="button-d4fb24">{t('reminders:form.cancel')}</Button>
          <Button variant="contained" onClick={handleCreate} disabled={creating} data-testid="button-33e032">
            {creating ? t('reminders:form.creating') : t('reminders:form.submit')}
          </Button>
        </DialogActions>
      </Dialog>

      {/* Dismiss / Complete confirmation */}
      <Dialog open={!!actionTarget} onClose={() => setActionTarget(null)} maxWidth="xs" fullWidth data-testid="dialog-db5ec1">
        <DialogTitle>
          {actionTarget?.kind === 'dismiss' ? t('reminders:actions.dismiss') : t('reminders:actions.complete')}
        </DialogTitle>
        <DialogContent sx={{ pt: '16px !important' }}>
          {actionError && <Alert severity="error" sx={{ mb: 1 }}>{actionError}</Alert>}
          <TextField label={t('reminders:actions.note')} value={actionNote} onChange={e => setActionNote(e.target.value)} size="small" fullWidth multiline rows={2}  data-testid="textfield-d1c4c7" />
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setActionTarget(null)} data-testid="button-4f14ce">{t('reminders:form.cancel')}</Button>
          <Button variant="contained" onClick={handleAction} disabled={actioning} data-testid="button-120bde">
            {actionTarget?.kind === 'dismiss' ? t('reminders:actions.dismiss') : t('reminders:actions.complete')}
          </Button>
        </DialogActions>
      </Dialog>
    </Box>
  )
}

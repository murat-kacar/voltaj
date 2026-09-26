import { useEffect, useState } from 'react'
import { Paper, Button, Stack, Typography } from '@mui/material'
import PlayArrowIcon from '@mui/icons-material/PlayArrow'
import { auditLogsApi, operationsApi, type AuditLogDto, type DeadLetterMessage } from '../../api'
import { useI18n } from '../../i18n'
import { DataGrid, type GridColDef } from '@mui/x-data-grid'

/** The record of what was done, newest first; the Settings page gives it its heading. */
export function AuditLogPanel() {
  const { translate: t } = useI18n()
  const [rows, setRows] = useState<AuditLogDto[]>([])
  const [loading, setLoading] = useState(true)

  useEffect(() => {
    let ignore = false
    auditLogsApi.listRecent()
      .then(items => { if (!ignore) { setRows(items); setLoading(false); } })
      .catch(() => { if (!ignore) setLoading(false); })
    return () => { ignore = true }
  }, [])

  const columns: GridColDef[] = [
    { field: 'timestamp', headerName: t('common:settings.auditLog.time'), width: 180, type: 'dateTime', valueGetter: (val) => new Date(val) },
    { field: 'action', headerName: t('common:settings.auditLog.action'), width: 200 },
    { field: 'entityName', headerName: t('common:settings.auditLog.entity'), width: 150 },
    { field: 'entityId', headerName: t('common:settings.auditLog.entityId'), width: 250 },
    { field: 'details', headerName: t('common:settings.auditLog.details'), flex: 1 },
  ]

  return (
    <Paper sx={{ width: '100%', height: 600 }}>
      <DataGrid
        rows={rows}
        columns={columns}
        loading={loading}
        initialState={{ pagination: { paginationModel: { pageSize: 15 } } }}
        pageSizeOptions={[15, 50, 100]}
        disableRowSelectionOnClick
      />
    </Paper>
  )
}

export function DeadLetterQueuePanel() {
  const [rows, setRows] = useState<DeadLetterMessage[]>([])
  const [loading, setLoading] = useState(true)

  const loadData = () => {
    setLoading(true)
    operationsApi.listDeadLetters()
      .then(items => { setRows(items); setLoading(false) })
      .catch(() => setLoading(false))
  }

  useEffect(() => { loadData() }, [])

  const handleReplay = async (id: string) => {
    try {
      await operationsApi.replayDeadLetter(id)
      loadData()
    } catch {
      // ignore
    }
  }

  const columns: GridColDef[] = [
    { field: 'occurredOn', headerName: 'Oluşma Zamanı', width: 180, type: 'dateTime', valueGetter: (val) => new Date(val) },
    { field: 'eventType', headerName: 'Olay (Event)', width: 250 },
    { field: 'aggregateType', headerName: 'Kaynak (Aggregate)', width: 150 },
    { field: 'error', headerName: 'Hata Mesajı', flex: 1 },
    {
      field: 'actions',
      headerName: '',
      width: 150,
      renderCell: (params) => (
        <Button size="small" variant="outlined" color="error" startIcon={<PlayArrowIcon />} onClick={() => handleReplay(params.row.id)}>
          Yeniden Dene
        </Button>
      )
    }
  ]

  return (
    <Paper sx={{ width: '100%', height: 400, mt: 4, mb: 4, display: 'flex', flexDirection: 'column', p: 2 }}>
      <Stack direction="row" sx={{ mb: 2, alignItems: 'center', justifyContent: 'space-between' }}>
        <Typography variant="h6" color="error">Kuyruk Yönetimi (Dead Letter Queue)</Typography>
        <Button size="small" onClick={loadData}>Yenile</Button>
      </Stack>
      <DataGrid
        rows={rows}
        columns={columns}
        loading={loading}
        initialState={{ pagination: { paginationModel: { pageSize: 10 } } }}
        pageSizeOptions={[10, 20]}
        disableRowSelectionOnClick
      />
    </Paper>
  )
}

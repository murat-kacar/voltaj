import { useEffect, useState } from 'react'
import { Paper } from '@mui/material'
import { auditLogsApi, type AuditLogDto } from './api'
import { useI18n } from './i18n'
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

import { useMemo } from 'react'
import { Alert, Paper } from '@mui/material'
import { DataGrid, type GridColDef, type GridRowId, type GridValidRowModel } from '@mui/x-data-grid'
import { trTR } from '@mui/x-data-grid/locales'
import { useI18n } from '../i18n'
import type { PagedQuery } from './usePagedQuery'

type Props<T extends GridValidRowModel> = {
  columns: GridColDef<T>[]
  query: PagedQuery<T>
  pageSizeOptions?: number[]
  /** What an empty list says. */
  emptyText?: string
  /** Makes the rows clickable. */
  onRowClick?: (row: T) => void
  getRowId?: (row: T) => GridRowId
}

/** A table of a paged list: pages come from the server, the pager and the empty state speak the language of the screen. */
export function PagedGrid<T extends GridValidRowModel>({ columns, query, pageSizeOptions = [25, 50, 100], emptyText, onRowClick, getRowId }: Props<T>) {
  const { lang } = useI18n()
  // the server decides the order and the filters, so the columns neither sort nor filter on their own
  const fixedColumns = useMemo(() => columns.map((column) => ({ sortable: false, filterable: false, ...column })), [columns])
  const localeText = useMemo(
    () => ({
      ...(lang === 'tr' ? trTR.components.MuiDataGrid.defaultProps.localeText : {}),
      ...(emptyText ? { noRowsLabel: emptyText } : {}),
    }),
    [lang, emptyText],
  )

  return (
    <>
      {query.error && <Alert severity="error" sx={{ mb: 2 }}>{query.error}</Alert>}
      <Paper sx={{ width: '100%' }}>
        <DataGrid
          rows={query.rows}
          columns={fixedColumns}
          loading={query.loading}
          getRowId={getRowId}
          paginationMode="server"
          rowCount={query.total}
          paginationModel={query.paging}
          onPaginationModelChange={query.setPaging}
          pageSizeOptions={pageSizeOptions}
          onRowClick={onRowClick ? (params) => onRowClick(params.row as T) : undefined}
          disableRowSelectionOnClick
          disableColumnFilter
          disableColumnMenu
          autoHeight
          localeText={localeText}
          sx={onRowClick ? { '& .MuiDataGrid-row': { cursor: 'pointer' } } : undefined}
        />
      </Paper>
    </>
  )
}

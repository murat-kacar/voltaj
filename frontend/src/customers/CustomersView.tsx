import { useMemo, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { Button, Chip, InputAdornment, MenuItem, Stack, TextField } from '@mui/material'
import AddIcon from '@mui/icons-material/Add'
import SearchIcon from '@mui/icons-material/Search'
import type { GridColDef } from '@mui/x-data-grid'
import { customersApi, sessionRoles, type Customer } from '../api'
import { PageHeader } from '../common/PageHeader'
import { PagedGrid } from '../common/PagedGrid'
import { usePagedQuery } from '../common/usePagedQuery'
import { formatDate } from '../i18n/formatters'
import { useI18n } from '../i18n'
import { CustomerFormDialog } from './CustomerFormDialog'

/** Everyone the business sells to or works for: found by any of their details, opened for the full 360 page. */
export function CustomersView() {
  const { translate: t, lang } = useI18n()
  const navigate = useNavigate()
  const canEdit = useMemo(() => sessionRoles().some((role) => role === 'Admin' || role === 'Manager'), [])
  const [search, setSearch] = useState('')
  const [type, setType] = useState('')
  const [status, setStatus] = useState('active')
  const [creating, setCreating] = useState(false)

  const query = usePagedQuery<Customer>(
    (limit, offset) =>
      customersApi.page({
        search: search.trim() || undefined,
        type: type || undefined,
        active: status === 'all' ? undefined : status === 'active',
        limit,
        offset,
      }),
    `${search}|${type}|${status}`,
  )

  const columns = useMemo<GridColDef<Customer>[]>(
    () => [
      { field: 'fullName', headerName: t('customers:table.name'), flex: 1, minWidth: 200 },
      { field: 'phone', headerName: t('customers:table.phone'), width: 150 },
      { field: 'email', headerName: t('customers:table.email'), flex: 1, minWidth: 180 },
      { field: 'taxNumber', headerName: t('customers:table.taxNumber'), width: 130 },
      {
        field: 'type',
        headerName: t('customers:table.type'),
        width: 120,
        renderCell: (params) => <Chip size="small" color={params.row.type === 'Active' ? 'success' : 'default'} label={t(`customers:type.${params.row.type}`)} />,
      },
      {
        field: 'isActive',
        headerName: t('customers:table.status'),
        width: 110,
        renderCell: (params) => (
          <Chip size="small" color={params.row.isActive ? 'default' : 'warning'} variant={params.row.isActive ? 'outlined' : 'filled'} label={params.row.isActive ? t('customers:status.active') : t('customers:status.inactive')} />
        ),
      },
      {
        field: 'createdAt',
        headerName: t('customers:table.created'),
        width: 130,
        renderCell: (params) => formatDate(params.row.createdAt, lang, { year: 'numeric', month: 'short', day: 'numeric' }),
      },
    ],
    [t, lang],
  )

  return (
    <div>
      <PageHeader
        overline={t('customers:eyebrow')}
        title={t('customers:title')}
        actions={canEdit && <Button variant="contained" startIcon={<AddIcon />} onClick={() => setCreating(true)} data-testid="button-b0e116">{t('customers:newCustomer')}</Button>}
      />

      <Stack direction={{ xs: 'column', md: 'row' }} spacing={2} sx={{ mb: 2 }}>
        <TextField
          size="small"
          placeholder={t('customers:searchPlaceholder')}
          value={search}
          onChange={(event) => setSearch(event.target.value)}
          sx={{ flexGrow: 1, maxWidth: 440 }}
          slotProps={{ input: { startAdornment: <InputAdornment position="start"><SearchIcon /></InputAdornment> } }}
         data-testid="textfield-e3e727" />
        <TextField size="small" select value={type} onChange={(event) => setType(event.target.value)} sx={{ minWidth: 170 }} slotProps={{ select: { displayEmpty: true } }} data-testid="textfield-aa889a">
          <MenuItem value="" data-testid="menuitem-7b011d">{t('customers:filters.allTypes')}</MenuItem>
          <MenuItem value="Lead" data-testid="menuitem-ae18c9">{t('customers:type.Lead')}</MenuItem>
          <MenuItem value="Active" data-testid="menuitem-c0c2de">{t('customers:type.Active')}</MenuItem>
        </TextField>
        <TextField size="small" select value={status} onChange={(event) => setStatus(event.target.value)} sx={{ minWidth: 170 }} data-testid="textfield-122048">
          <MenuItem value="active" data-testid="menuitem-863367">{t('customers:status.active')}</MenuItem>
          <MenuItem value="inactive" data-testid="menuitem-3745ae">{t('customers:status.inactive')}</MenuItem>
          <MenuItem value="all" data-testid="menuitem-fe9998">{t('customers:filters.allStatuses')}</MenuItem>
        </TextField>
      </Stack>

      <PagedGrid columns={columns} query={query} emptyText={t('customers:table.empty')} onRowClick={(row) => navigate('/customers/' + row.id)} />

      {creating && (
        <CustomerFormDialog
          customer={null}
          onClose={() => setCreating(false)}
          onSaved={(created) => {
            setCreating(false)
            navigate('/customers/' + created.id)
          }}
        />
      )}
    </div>
  )
}

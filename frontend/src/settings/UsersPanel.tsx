import { useState } from 'react'
import { Alert, Box, Button, Chip, MenuItem, Stack, TextField } from '@mui/material'
import type { GridColDef } from '@mui/x-data-grid'
import { usersApi, type UserSummary } from '../api'
import { errorText } from '../common/errors'
import { PagedGrid } from '../common/PagedGrid'
import { usePagedQuery } from '../common/usePagedQuery'
import { useI18n } from '../i18n'

/** The roles the shop hands out. Approving a user already gives them Viewer, and the API turns down a name it does not know. */
const ASSIGNABLE_ROLES = ['Admin', 'Manager', 'Technician']

type Filter = 'all' | 'waiting'

/** Who may sign in: the users, the ones still waiting for approval, and the roles each holds. */
export function UsersPanel() {
  const { translate: t } = useI18n()
  const [filter, setFilter] = useState<Filter>('all')
  const [busyId, setBusyId] = useState<string | null>(null)
  const [actionError, setActionError] = useState('')

  const query = usePagedQuery<UserSummary>(
    (limit, offset) => usersApi.page({ approved: filter === 'waiting' ? false : undefined, limit, offset }),
    filter,
  )

  const run = async (id: string, action: () => Promise<unknown>) => {
    setBusyId(id)
    setActionError('')
    try {
      await action()
      query.reload()
    } catch (reason) {
      setActionError(errorText(reason))
    } finally {
      setBusyId(null)
    }
  }

  const columns: GridColDef<UserSummary>[] = [
    { field: 'name', headerName: t('common:settings.users.table.name'), flex: 1, minWidth: 160 },
    { field: 'email', headerName: t('common:settings.users.table.email'), flex: 1, minWidth: 200 },
    {
      field: 'isApproved',
      headerName: t('common:settings.users.table.status'),
      width: 190,
      renderCell: (params) => (
        <Chip
          size="small"
          color={params.row.isApproved ? 'success' : 'warning'}
          label={params.row.isApproved ? t('common:settings.users.approved') : t('common:settings.users.waiting')}
        />
      ),
    },
    {
      field: 'roles',
      headerName: t('common:settings.users.table.roles'),
      flex: 1,
      minWidth: 200,
      renderCell: (params) => (
        <Stack direction="row" spacing={0.5} sx={{ alignItems: 'center', height: '100%' }}>
          {params.row.roles.map((role) => <Chip key={role} size="small" label={role} />)}
        </Stack>
      ),
    },
    {
      field: 'actions',
      headerName: t('common:settings.users.table.actions'),
      width: 300,
      renderCell: (params) => {
        const user = params.row
        const busy = busyId === user.id
        return (
          <Stack direction="row" spacing={1} sx={{ alignItems: 'center', height: '100%' }}>
            {!user.isApproved && (
              <Button size="small" variant="contained" disabled={busy} onClick={() => run(user.id, () => usersApi.approve(user.id))}>
                {t('common:settings.users.approve')}
              </Button>
            )}
            <TextField
              select
              size="small"
              value=""
              disabled={busy}
              onChange={(event) => run(user.id, () => usersApi.assignRole(user.id, event.target.value))}
              slotProps={{ select: { displayEmpty: true, renderValue: () => t('common:settings.users.addRole') } }}
              sx={{ minWidth: 150 }}
            >
              {ASSIGNABLE_ROLES.filter((role) => !user.roles.includes(role)).map((role) => (
                <MenuItem key={role} value={role}>{role}</MenuItem>
              ))}
            </TextField>
          </Stack>
        )
      },
    },
  ]

  return (
    <Box>
      <TextField
        select
        size="small"
        label={t('common:settings.users.filter.label')}
        value={filter}
        onChange={(event) => setFilter(event.target.value as Filter)}
        sx={{ mb: 2, minWidth: 220 }}
        data-testid="users-filter"
      >
        <MenuItem value="all">{t('common:settings.users.filter.all')}</MenuItem>
        <MenuItem value="waiting">{t('common:settings.users.filter.waiting')}</MenuItem>
      </TextField>

      {actionError && <Alert severity="error" sx={{ mb: 2 }}>{actionError}</Alert>}

      <PagedGrid columns={columns} query={query} emptyText={t('common:settings.users.empty')} />
    </Box>
  )
}

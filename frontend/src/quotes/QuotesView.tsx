import { useMemo, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { Box, Button, InputAdornment, MenuItem, Stack, TextField } from '@mui/material'
import AddIcon from '@mui/icons-material/Add'
import SearchIcon from '@mui/icons-material/Search'
import type { GridColDef } from '@mui/x-data-grid'
import { sessionRoles, type QuoteState, type QuoteSummary } from '../api'
import { formatDay, formatMoney } from '../common/format'
import { PageHeader } from '../common/PageHeader'
import { PagedGrid } from '../common/PagedGrid'
import { usePagedQuery } from '../common/usePagedQuery'
import { quotesApi } from '../api'
import { useI18n } from '../i18n'
import { QuoteFormDialog } from './QuoteFormDialog'
import { QuoteStateChip } from './QuoteStateChip'
import { isLapsed } from './quoteMath'

const states: QuoteState[] = ['Draft', 'Issued', 'Accepted', 'Rejected', 'Expired']

/** The offers made to customers: found by number, title or customer, opened for the whole quote and what to do with it next. */
export function QuotesView() {
  const { translate: t, lang } = useI18n()
  const navigate = useNavigate()
  const canEdit = useMemo(() => sessionRoles().some((role) => role === 'Admin' || role === 'Manager'), [])
  const [search, setSearch] = useState('')
  const [state, setState] = useState('')
  const [creating, setCreating] = useState(false)

  const query = usePagedQuery<QuoteSummary>(
    (limit, offset) => quotesApi.page({ search: search.trim() || undefined, state: state || undefined, limit, offset }),
    `${search}|${state}`,
  )

  const columns = useMemo<GridColDef<QuoteSummary>[]>(
    () => [
      { field: 'number', headerName: t('quotes:table.number'), width: 120, renderCell: (params) => <strong>{params.row.number}</strong> },
      { field: 'customerName', headerName: t('quotes:table.customer'), flex: 1, minWidth: 150 },
      { field: 'title', headerName: t('quotes:table.title'), flex: 1.2, minWidth: 160 },
      {
        field: 'total',
        headerName: t('quotes:table.total'),
        width: 120,
        align: 'right',
        headerAlign: 'right',
        renderCell: (params) => formatMoney(params.row.total, lang),
      },
      {
        field: 'validUntil',
        headerName: t('quotes:table.validUntil'),
        width: 120,
        renderCell: (params) =>
          params.row.validUntil ? (
            <Box component="span" sx={{ color: isLapsed(params.row.state, params.row.validUntil) ? 'error.main' : 'inherit' }}>
              {formatDay(params.row.validUntil, lang)}
            </Box>
          ) : (
            '—'
          ),
      },
      {
        field: 'deposit',
        headerName: t('quotes:table.deposit'),
        width: 175,
        renderCell: (params) =>
          params.row.requiredDepositAmount > 0 ? `${formatMoney(params.row.depositPaidAmount, lang)} / ${formatMoney(params.row.requiredDepositAmount, lang)}` : '—',
      },
      {
        field: 'state',
        headerName: t('quotes:table.state'),
        width: 135,
        renderCell: (params) => <QuoteStateChip state={params.row.state} validUntil={params.row.validUntil} />,
      },
    ],
    [t, lang],
  )

  return (
    <div>
      <PageHeader
        overline={t('quotes:eyebrow')}
        title={t('quotes:title')}
        actions={canEdit && <Button variant="contained" startIcon={<AddIcon />} onClick={() => setCreating(true)}>{t('quotes:newQuote')}</Button>}
      />

      <Stack direction={{ xs: 'column', md: 'row' }} spacing={2} sx={{ mb: 2 }}>
        <TextField
          size="small"
          placeholder={t('quotes:searchPlaceholder')}
          value={search}
          onChange={(event) => setSearch(event.target.value)}
          sx={{ flexGrow: 1, maxWidth: 440 }}
          slotProps={{ input: { startAdornment: <InputAdornment position="start"><SearchIcon /></InputAdornment> } }}
        />
        <TextField size="small" select value={state} onChange={(event) => setState(event.target.value)} sx={{ minWidth: 190 }} slotProps={{ select: { displayEmpty: true } }}>
          <MenuItem value="">{t('quotes:filters.allStates')}</MenuItem>
          {states.map((option) => (
            <MenuItem key={option} value={option}>{t(`quotes:state.${option}`)}</MenuItem>
          ))}
        </TextField>
      </Stack>

      <PagedGrid columns={columns} query={query} emptyText={t('quotes:table.empty')} onRowClick={(row) => navigate('/quotes/' + row.id)} />

      {creating && (
        <QuoteFormDialog
          quote={null}
          onClose={() => setCreating(false)}
          onSaved={(created) => {
            setCreating(false)
            navigate('/quotes/' + created.id)
          }}
        />
      )}
    </div>
  )
}

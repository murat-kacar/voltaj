import { useMemo, useState } from 'react'
import { Box, IconButton, InputAdornment, Stack, TextField, Typography } from '@mui/material'
import Inventory2Icon from '@mui/icons-material/Inventory2'
import SearchIcon from '@mui/icons-material/Search'
import type { GridColDef } from '@mui/x-data-grid'
import { inventoryApi, sessionRoles, type StockDto } from '../../api'
import { AmountField } from '../../common/AmountField'
import { FormDialog } from '../../common/FormDialog'
import { PageHeader } from '../../common/PageHeader'
import { PagedGrid } from '../../common/PagedGrid'
import { usePagedQuery } from '../../common/usePagedQuery'
import { useI18n } from '../../i18n'

/** What is on the shelf: how many of each material, how many are promised, how many are free. The items themselves are the catalog's business. */
export function StockView() {
  const { translate: t } = useI18n()
  const isManager = useMemo(() => sessionRoles().some((role) => role === 'Admin' || role === 'Manager'), [])
  const [search, setSearch] = useState('')
  const [adjusting, setAdjusting] = useState<StockDto | null>(null)

  const query = usePagedQuery<StockDto>((limit, offset) => inventoryApi.list({ search: search.trim() || undefined, limit, offset }), search)

  const columns = useMemo<GridColDef<StockDto>[]>(() => {
    const list: GridColDef<StockDto>[] = [
      { field: 'materialCode', headerName: t('common:stock.code'), width: 150 },
      { field: 'name', headerName: t('common:stock.name'), flex: 1, minWidth: 180 },
      { field: 'quantityOnHand', headerName: t('common:stock.onHand'), width: 120, type: 'number' },
      { field: 'reservedQuantity', headerName: t('common:stock.reserved'), width: 120, type: 'number' },
      { field: 'availableQuantity', headerName: t('common:stock.available'), width: 130, type: 'number' },
    ]
    if (isManager) {
      list.push({
        field: 'actions',
        headerName: '',
        width: 70,
        renderCell: (params) => (
          <IconButton size="small" title={t('common:stock.adjust')} onClick={() => setAdjusting(params.row)} data-testid={`stock-adjust-${params.row.materialCode}`}>
            <Inventory2Icon fontSize="small" />
          </IconButton>
        ),
      })
    }
    return list
  }, [t, isManager])

  return (
    <Box>
      <PageHeader overline={t('common:nav.catalogStock')} title={t('common:nav.stock')} />

      <Stack direction="row" spacing={2} sx={{ mb: 2, alignItems: 'center' }}>
        <TextField
          size="small"
          placeholder={t('common:stock.search')}
          value={search}
          onChange={(event) => setSearch(event.target.value)}
          sx={{ flexGrow: 1, maxWidth: 420 }}
          slotProps={{ input: { startAdornment: <InputAdornment position="start"><SearchIcon /></InputAdornment> } }}
         data-testid="textfield-bc4bc0" />
        <Box sx={{ flexGrow: 1 }} />
        {!isManager && <Typography variant="caption" color="text.secondary">{t('common:stock.readOnly')}</Typography>}
      </Stack>

      <PagedGrid columns={columns} query={query} emptyText={t('common:stock.empty')} getRowId={(row) => row.materialCode} />

      {adjusting && (
        <StockAdjustDialog
          stock={adjusting}
          onClose={() => setAdjusting(null)}
          onSaved={() => {
            setAdjusting(null)
            query.reload()
          }}
        />
      )}
    </Box>
  )
}

function StockAdjustDialog({ stock, onClose, onSaved }: { stock: StockDto; onClose: () => void; onSaved: () => void }) {
  const { translate: t } = useI18n()
  const [delta, setDelta] = useState(0)

  return (
    <FormDialog
      title={`${t('common:stock.adjust')} · ${stock.name}`}
      submitLabel={t('common:actions.save')}
      canSubmit={delta !== 0}
      onClose={onClose}
      onSubmit={async () => {
        await inventoryApi.adjust({ materialCode: stock.materialCode, delta })
        onSaved()
      }}
    >
      <Typography variant="body2" color="text.secondary">{t('common:stock.adjustHint')}</Typography>
      <Typography>{`${t('common:stock.onHand')}: ${stock.quantityOnHand}`}</Typography>
      <AmountField autoFocus label={t('common:stock.adjustDelta')} value={delta} onChange={setDelta} />
    </FormDialog>
  )
}

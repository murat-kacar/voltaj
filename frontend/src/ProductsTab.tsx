import { useMemo, useState } from 'react'
import { Box, Button, Chip, FormControlLabel, IconButton, InputAdornment, MenuItem, Stack, Switch, TextField, Typography } from '@mui/material'
import AddIcon from '@mui/icons-material/Add'
import EditIcon from '@mui/icons-material/Edit'
import SearchIcon from '@mui/icons-material/Search'
import type { GridColDef } from '@mui/x-data-grid'
import { productsApi, type Product } from './api'
import { AmountField } from './common/AmountField'
import { formatMoney } from './common/format'
import { FormDialog } from './common/FormDialog'
import { PagedGrid } from './common/PagedGrid'
import { usePagedQuery } from './common/usePagedQuery'
import { useI18n } from './i18n'

const VAT_RATES = [0, 1, 10, 20]

/** The price list: what the counter sells, at what price (VAT included), under which barcode. */
export function ProductsTab({ isManager }: { isManager: boolean }) {
  const { translate: t, lang } = useI18n()
  const [search, setSearch] = useState('')
  const [editing, setEditing] = useState<Product | 'new' | null>(null)

  const query = usePagedQuery<Product>((limit, offset) => productsApi.list({ search: search.trim() || undefined, limit, offset }), search)

  const columns = useMemo<GridColDef<Product>[]>(() => {
    const list: GridColDef<Product>[] = [
      { field: 'code', headerName: t('common:sales.products.code'), width: 130 },
      { field: 'name', headerName: t('common:sales.products.name'), flex: 1, minWidth: 180 },
      { field: 'barcode', headerName: t('common:sales.products.barcode'), width: 160 },
      { field: 'unit', headerName: t('common:sales.products.unit'), width: 80 },
      {
        field: 'salePrice',
        headerName: t('common:sales.products.price'),
        width: 150,
        align: 'right',
        headerAlign: 'right',
        renderCell: (params) => formatMoney(params.row.salePrice, lang),
      },
      { field: 'vatRate', headerName: t('common:sales.products.vat'), width: 90, align: 'right', headerAlign: 'right' },
      {
        field: 'stockAvailable',
        headerName: t('common:sales.products.stock'),
        width: 90,
        align: 'right',
        headerAlign: 'right',
        renderCell: (params) => (params.row.tracksStock ? params.row.stockAvailable ?? 0 : ''),
      },
      {
        field: 'isActive',
        headerName: t('common:status.active'),
        width: 100,
        renderCell: (params) => (
          <Chip size="small" color={params.row.isActive ? 'success' : 'default'} label={params.row.isActive ? t('common:sales.products.active') : t('common:sales.products.inactive')} />
        ),
      },
    ]
    if (isManager) {
      list.push({
        field: 'actions',
        headerName: '',
        width: 70,
        renderCell: (params) => (
          <IconButton size="small" title={t('common:sales.products.edit')} onClick={() => setEditing(params.row)} data-testid="iconbutton-ce0f24"><EditIcon fontSize="small" /></IconButton>
        ),
      })
    }
    return list
  }, [t, lang, isManager])

  return (
    <Box>
      <Stack direction="row" spacing={2} sx={{ mb: 2, alignItems: 'center' }}>
        <TextField
          size="small"
          placeholder={t('common:sales.products.search')}
          value={search}
          onChange={(event) => setSearch(event.target.value)}
          sx={{ flexGrow: 1, maxWidth: 420 }}
          slotProps={{ input: { startAdornment: <InputAdornment position="start"><SearchIcon /></InputAdornment> } }}
         data-testid="textfield-0d84e2" />
        <Box sx={{ flexGrow: 1 }} />
        {isManager ? (
          <Button variant="contained" startIcon={<AddIcon />} onClick={() => setEditing('new')} data-testid="button-1eedf5">{t('common:sales.products.new')}</Button>
        ) : (
          <Typography variant="caption" color="text.secondary">{t('common:sales.products.readOnly')}</Typography>
        )}
      </Stack>

      <PagedGrid columns={columns} query={query} />

      {editing && (
        <ProductDialog
          product={editing === 'new' ? null : editing}
          onClose={() => setEditing(null)}
          onSaved={() => {
            setEditing(null)
            query.reload()
          }}
        />
      )}
    </Box>
  )
}

function ProductDialog({ product, onClose, onSaved }: { product: Product | null; onClose: () => void; onSaved: () => void }) {
  const { translate: t } = useI18n()
  const [code, setCode] = useState(product?.code ?? '')
  const [name, setName] = useState(product?.name ?? '')
  const [barcode, setBarcode] = useState(product?.barcode ?? '')
  const [unit, setUnit] = useState(product?.unit ?? 'adet')
  const [price, setPrice] = useState(product?.salePrice ?? 0)
  const [vatRate, setVatRate] = useState(product?.vatRate ?? 20)
  const [tracksStock, setTracksStock] = useState(product?.tracksStock ?? true)
  const [isActive, setIsActive] = useState(product?.isActive ?? true)

  // a rate the list does not offer (say 8) stays selectable when it is the product's own
  const rates = VAT_RATES.includes(vatRate) ? VAT_RATES : [...VAT_RATES, vatRate].sort((left, right) => left - right)

  return (
    <FormDialog
      title={product ? t('common:sales.products.edit') : t('common:sales.products.new')}
      submitLabel={t('common:actions.save')}
      canSubmit={code.trim() !== '' && name.trim() !== '' && price >= 0}
      onClose={onClose}
      onSubmit={async () => {
        const common = { name: name.trim(), barcode: barcode.trim() || undefined, unit: unit.trim() || undefined, salePrice: price, vatRate, tracksStock }
        if (product) await productsApi.update(product.id, { ...common, isActive })
        else await productsApi.create({ ...common, code: code.trim() })
        onSaved()
      }}
    >
      <TextField
        required
        autoFocus={!product}
        label={t('common:sales.products.code')}
        value={code}
        disabled={product !== null}
        helperText={product ? t('common:sales.products.codeLocked') : undefined}
        onChange={(event) => setCode(event.target.value)}
       data-testid="textfield-8707a3" />
      <TextField required autoFocus={product !== null} label={t('common:sales.products.name')} value={name} onChange={(event) => setName(event.target.value)}  data-testid="textfield-1fe651" />
      <TextField label={t('common:sales.products.barcode')} value={barcode} onChange={(event) => setBarcode(event.target.value)}  data-testid="textfield-dd0f42" />
      <Stack direction="row" spacing={2}>
        <TextField label={t('common:sales.products.unit')} value={unit} onChange={(event) => setUnit(event.target.value)} sx={{ width: 120 }}  data-testid="textfield-fea42b" />
        <AmountField fullWidth label={t('common:sales.products.price')} value={price} onChange={setPrice} />
      </Stack>
      <TextField select label={t('common:sales.products.vat')} value={vatRate} onChange={(event) => setVatRate(Number(event.target.value))} data-testid="textfield-4323b4">
        {rates.map((rate) => <MenuItem key={rate} value={rate} data-testid="menuitem-2fdf43">{`%${rate}`}</MenuItem>)}
      </TextField>
      <FormControlLabel control={<Switch checked={tracksStock} onChange={(event) => setTracksStock(event.target.checked)} />} label={t('common:sales.products.tracksStock')} />
      {product && <FormControlLabel control={<Switch checked={isActive} onChange={(event) => setIsActive(event.target.checked)} />} label={t('common:sales.products.active')} />}
    </FormDialog>
  )
}

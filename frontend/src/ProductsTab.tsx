import { useEffect, useMemo, useState } from 'react'
import {
  Alert,
  Box,
  Button,
  Chip,
  CircularProgress,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  FormControlLabel,
  IconButton,
  InputAdornment,
  MenuItem,
  Paper,
  Stack,
  Switch,
  TextField,
  Typography,
} from '@mui/material'
import AddIcon from '@mui/icons-material/Add'
import EditIcon from '@mui/icons-material/Edit'
import InventoryIcon from '@mui/icons-material/Inventory2'
import SearchIcon from '@mui/icons-material/Search'
import { DataGrid, type GridColDef, type GridPaginationModel } from '@mui/x-data-grid'
import { inventoryApi, productsApi, type Product } from './api'
import { AmountField } from './AmountField'
import { useI18n } from './i18n'
import { errorText, formatMoney, gridLocaleText } from './quickSaleUtils'

const VAT_RATES = [0, 1, 10, 20]

/** The price list: what the counter sells, at what price (VAT included), under which barcode. */
export function ProductsTab({ isManager }: { isManager: boolean }) {
  const { translate: t, lang } = useI18n()
  const [search, setSearch] = useState('')
  const [paging, setPaging] = useState<GridPaginationModel>({ page: 0, pageSize: 25 })
  const [rows, setRows] = useState<Product[]>([])
  const [total, setTotal] = useState(0)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')
  const [editing, setEditing] = useState<Product | 'new' | null>(null)
  const [stockFor, setStockFor] = useState<Product | null>(null)
  const [reloadKey, setReloadKey] = useState(0)

  useEffect(() => {
    let ignore = false
    const handle = window.setTimeout(() => {
      setLoading(true)
      productsApi
        .list({ search: search.trim() || undefined, limit: paging.pageSize, offset: paging.page * paging.pageSize })
        .then((page) => {
          if (ignore) return
          setRows(page.items)
          setTotal(page.total)
          setError('')
        })
        .catch((reason: unknown) => {
          if (!ignore) setError(errorText(reason))
        })
        .finally(() => {
          if (!ignore) setLoading(false)
        })
    }, 250)
    return () => {
      ignore = true
      window.clearTimeout(handle)
    }
  }, [search, paging, reloadKey])

  const columns = useMemo<GridColDef<Product>[]>(() => {
    const base = { sortable: false, filterable: false }
    const list: GridColDef<Product>[] = [
      { ...base, field: 'code', headerName: t('common:sales.products.code'), width: 130 },
      { ...base, field: 'name', headerName: t('common:sales.products.name'), flex: 1, minWidth: 180 },
      { ...base, field: 'barcode', headerName: t('common:sales.products.barcode'), width: 160 },
      { ...base, field: 'unit', headerName: t('common:sales.products.unit'), width: 80 },
      {
        ...base,
        field: 'salePrice',
        headerName: t('common:sales.products.price'),
        width: 150,
        align: 'right',
        headerAlign: 'right',
        renderCell: (params) => formatMoney(params.row.salePrice, lang),
      },
      { ...base, field: 'vatRate', headerName: t('common:sales.products.vat'), width: 90, align: 'right', headerAlign: 'right' },
      {
        ...base,
        field: 'stockAvailable',
        headerName: t('common:sales.products.stock'),
        width: 90,
        align: 'right',
        headerAlign: 'right',
        renderCell: (params) => (params.row.tracksStock ? params.row.stockAvailable ?? 0 : ''),
      },
      {
        ...base,
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
        ...base,
        field: 'actions',
        headerName: '',
        width: 110,
        renderCell: (params) => (
          <Box>
            <IconButton size="small" title={t('common:sales.products.edit')} onClick={() => setEditing(params.row)}><EditIcon fontSize="small" /></IconButton>
            {params.row.tracksStock && (
              <IconButton size="small" title={t('common:sales.products.adjustStock')} onClick={() => setStockFor(params.row)}><InventoryIcon fontSize="small" /></IconButton>
            )}
          </Box>
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
          onChange={(event) => {
            setSearch(event.target.value)
            setPaging((current) => ({ ...current, page: 0 }))
          }}
          sx={{ flexGrow: 1, maxWidth: 420 }}
          slotProps={{ input: { startAdornment: <InputAdornment position="start"><SearchIcon /></InputAdornment> } }}
        />
        <Box sx={{ flexGrow: 1 }} />
        {isManager ? (
          <Button variant="contained" startIcon={<AddIcon />} onClick={() => setEditing('new')}>{t('common:sales.products.new')}</Button>
        ) : (
          <Typography variant="caption" color="text.secondary">{t('common:sales.products.readOnly')}</Typography>
        )}
      </Stack>

      {error && <Alert severity="error" sx={{ mb: 2 }}>{error}</Alert>}

      <Paper sx={{ width: '100%' }}>
        <DataGrid
          rows={rows}
          columns={columns}
          loading={loading}
          paginationMode="server"
          rowCount={total}
          paginationModel={paging}
          onPaginationModelChange={setPaging}
          pageSizeOptions={[25, 50, 100]}
          disableRowSelectionOnClick
          disableColumnFilter
          disableColumnMenu
          autoHeight
          localeText={gridLocaleText(lang)}
        />
      </Paper>

      {editing && (
        <ProductDialog
          product={editing === 'new' ? null : editing}
          onClose={() => setEditing(null)}
          onSaved={() => {
            setEditing(null)
            setReloadKey((key) => key + 1)
          }}
        />
      )}
      {stockFor && (
        <StockDialog
          product={stockFor}
          onClose={() => setStockFor(null)}
          onSaved={() => {
            setStockFor(null)
            setReloadKey((key) => key + 1)
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
  const [busy, setBusy] = useState(false)
  const [error, setError] = useState('')

  // a rate the list does not offer (say 8) stays selectable when it is the product's own
  const rates = VAT_RATES.includes(vatRate) ? VAT_RATES : [...VAT_RATES, vatRate].sort((left, right) => left - right)
  const valid = code.trim() !== '' && name.trim() !== '' && price >= 0

  const submit = async () => {
    setBusy(true)
    setError('')
    try {
      const common = { name: name.trim(), barcode: barcode.trim() || undefined, unit: unit.trim() || undefined, salePrice: price, vatRate, tracksStock }
      if (product) await productsApi.update(product.id, { ...common, isActive })
      else await productsApi.create({ ...common, code: code.trim() })
      onSaved()
    } catch (failure) {
      setError(errorText(failure))
      setBusy(false)
    }
  }

  return (
    <Dialog open onClose={onClose} maxWidth="xs" fullWidth>
      <DialogTitle>{product ? t('common:sales.products.edit') : t('common:sales.products.new')}</DialogTitle>
      <DialogContent>
        <Stack spacing={2} sx={{ mt: 1 }}>
          <TextField
            required
            autoFocus={!product}
            label={t('common:sales.products.code')}
            value={code}
            disabled={product !== null}
            helperText={product ? t('common:sales.products.codeLocked') : undefined}
            onChange={(event) => setCode(event.target.value)}
          />
          <TextField required autoFocus={product !== null} label={t('common:sales.products.name')} value={name} onChange={(event) => setName(event.target.value)} />
          <TextField label={t('common:sales.products.barcode')} value={barcode} onChange={(event) => setBarcode(event.target.value)} />
          <Stack direction="row" spacing={2}>
            <TextField label={t('common:sales.products.unit')} value={unit} onChange={(event) => setUnit(event.target.value)} sx={{ width: 120 }} />
            <AmountField fullWidth label={t('common:sales.products.price')} value={price} onChange={setPrice} />
          </Stack>
          <TextField select label={t('common:sales.products.vat')} value={vatRate} onChange={(event) => setVatRate(Number(event.target.value))}>
            {rates.map((rate) => <MenuItem key={rate} value={rate}>{`%${rate}`}</MenuItem>)}
          </TextField>
          <FormControlLabel control={<Switch checked={tracksStock} onChange={(event) => setTracksStock(event.target.checked)} />} label={t('common:sales.products.tracksStock')} />
          {product && <FormControlLabel control={<Switch checked={isActive} onChange={(event) => setIsActive(event.target.checked)} />} label={t('common:sales.products.active')} />}
          {error && <Alert severity="error">{error}</Alert>}
        </Stack>
      </DialogContent>
      <DialogActions>
        <Button onClick={onClose} disabled={busy}>{t('common:actions.cancel')}</Button>
        <Button variant="contained" disabled={busy || !valid} onClick={() => void submit()}>
          {busy ? <CircularProgress size={22} /> : t('common:actions.save')}
        </Button>
      </DialogActions>
    </Dialog>
  )
}

function StockDialog({ product, onClose, onSaved }: { product: Product; onClose: () => void; onSaved: () => void }) {
  const { translate: t } = useI18n()
  const [delta, setDelta] = useState(0)
  const [busy, setBusy] = useState(false)
  const [error, setError] = useState('')

  const submit = async () => {
    setBusy(true)
    setError('')
    try {
      await inventoryApi.adjust({ materialCode: product.code, delta })
      onSaved()
    } catch (failure) {
      setError(errorText(failure))
      setBusy(false)
    }
  }

  return (
    <Dialog open onClose={onClose} maxWidth="xs" fullWidth>
      <DialogTitle>{`${t('common:sales.products.adjustStock')} · ${product.name}`}</DialogTitle>
      <DialogContent>
        <Stack spacing={2} sx={{ mt: 1 }}>
          <Typography variant="body2" color="text.secondary">{t('common:sales.products.adjustHint')}</Typography>
          <Typography>{`${t('common:sales.products.stock')}: ${product.stockAvailable ?? 0}`}</Typography>
          <AmountField autoFocus label={t('common:sales.products.adjustDelta')} value={delta} onChange={setDelta} />
          {error && <Alert severity="error">{error}</Alert>}
        </Stack>
      </DialogContent>
      <DialogActions>
        <Button onClick={onClose} disabled={busy}>{t('common:actions.cancel')}</Button>
        <Button variant="contained" disabled={busy || delta === 0} onClick={() => void submit()}>
          {busy ? <CircularProgress size={22} /> : t('common:actions.save')}
        </Button>
      </DialogActions>
    </Dialog>
  )
}

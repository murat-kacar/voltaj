import { useEffect, useState } from 'react'
import {
  Box, Button, CircularProgress, Dialog, DialogActions, DialogContent,
  DialogTitle, InputAdornment, List, ListItemButton, ListItemText, TextField, Typography,
} from '@mui/material'
import SearchIcon from '@mui/icons-material/Search'
import { productsApi, type Product } from '../api'
import { formatMoney } from '../common/format'
import { useI18n } from '../i18n'
import { newLine, type QuoteLineDraft } from './quoteMath'

type Props = {
  onClose: () => void
  onAdd: (line: QuoteLineDraft) => void
}

export function ProductPickerDialog({ onClose, onAdd }: Props) {
  const { translate: t, lang } = useI18n()
  const [search, setSearch] = useState('')
  const [products, setProducts] = useState<Product[]>([])
  const [loading, setLoading] = useState(false)

  useEffect(() => {
    let ignore = false
    setLoading(true)
    productsApi.list({ search: search.trim() || undefined, activeOnly: true, limit: 50 })
      .then((page) => { if (!ignore) setProducts(page.items) })
      .finally(() => { if (!ignore) setLoading(false) })
    return () => { ignore = true }
  }, [search])

  const pick = (product: Product) => {
    onAdd({
      ...newLine(),
      description: product.name,
      unit: product.unit,
      unitPrice: product.salePrice,
      vatRate: product.vatRate,
      kind: 'Material',
    })
    onClose()
  }

  return (
    <Dialog open onClose={onClose} fullWidth maxWidth="xs">
      <DialogTitle>{t('quotes:form.catalog.title')}</DialogTitle>
      <DialogContent sx={{ display: 'flex', flexDirection: 'column', gap: 1.5, pt: '8px !important' }}>
        <TextField
          autoFocus
          size="small"
          placeholder={t('quotes:form.catalog.search')}
          value={search}
          onChange={(e) => setSearch(e.target.value)}
          slotProps={{ input: { startAdornment: <InputAdornment position="start"><SearchIcon /></InputAdornment> } }}
        />
        {loading ? (
          <Box sx={{ display: 'flex', justifyContent: 'center', p: 3 }}><CircularProgress size={24} /></Box>
        ) : products.length === 0 ? (
          <Typography color="text.secondary" sx={{ p: 2, textAlign: 'center' }}>{t('quotes:form.catalog.empty')}</Typography>
        ) : (
          <List dense disablePadding sx={{ maxHeight: 320, overflow: 'auto' }}>
            {products.map((product) => (
              <ListItemButton key={product.id} onClick={() => pick(product)}>
                <ListItemText
                  primary={product.name}
                  secondary={`${product.code} · ${product.unit} · ${formatMoney(product.salePrice, lang)}`}
                />
              </ListItemButton>
            ))}
          </List>
        )}
      </DialogContent>
      <DialogActions>
        <Button onClick={onClose}>{t('common:actions.cancel')}</Button>
      </DialogActions>
    </Dialog>
  )
}

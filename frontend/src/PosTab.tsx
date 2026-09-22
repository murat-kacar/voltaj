import { useEffect, useMemo, useRef, useState } from 'react'
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
  Divider,
  Grid,
  IconButton,
  InputAdornment,
  List,
  ListItemButton,
  ListItemText,
  MenuItem,
  Paper,
  Stack,
  TextField,
  Typography,
} from '@mui/material'
import AddIcon from '@mui/icons-material/Add'
import RemoveIcon from '@mui/icons-material/Remove'
import DeleteOutlineIcon from '@mui/icons-material/DeleteOutlined'
import QrCodeScannerIcon from '@mui/icons-material/QrCodeScanner'
import PrintIcon from '@mui/icons-material/Print'
import {
  ApiError,
  productsApi,
  quickSalesApi,
  type Customer,
  type Product,
  type QuickSale,
  type SalePaymentRequest,
  type ShiftReport,
} from './api'
import { AmountField } from './common/AmountField'
import { CustomerPicker } from './customers/CustomerPicker'
import { errorText } from './common/errors'
import { formatMoney } from './common/format'
import { Receipt } from './Receipt'
import { useI18n } from './i18n'
import { computeCart, fromCents, planPayments, toCents, type CartLine } from './saleMath'

const VAT_RATES = [0, 1, 10, 20]

type Props = {
  report: ShiftReport | null
  onSold: () => void
  onOpenShift: () => void
}

/** The register: find products, build the cart, take payment, print the receipt. */
export function PosTab({ report, onSold, onOpenShift }: Props) {
  const { translate: t, lang } = useI18n()
  const money = (cents: number) => formatMoney(fromCents(cents), lang)

  const scanRef = useRef<HTMLInputElement>(null)
  const [term, setTerm] = useState('')
  const [results, setResults] = useState<Product[]>([])
  const [searching, setSearching] = useState(false)
  const [notice, setNotice] = useState('')
  const [cart, setCart] = useState<CartLine[]>([])
  const [receiptDiscount, setReceiptDiscount] = useState(0)
  const [customer, setCustomer] = useState<Customer | null>(null)
  const [note, setNote] = useState('')
  const [cash, setCash] = useState(0)
  const [card, setCard] = useState(0)
  const [transfer, setTransfer] = useState(0)
  const [reference, setReference] = useState('')
  const [submitting, setSubmitting] = useState(false)
  const [error, setError] = useState('')
  const [completed, setCompleted] = useState<QuickSale | null>(null)
  const [freeLineOpen, setFreeLineOpen] = useState(false)

  const totals = useMemo(() => computeCart(cart, toCents(receiptDiscount)), [cart, receiptDiscount])
  const plan = useMemo(
    () => planPayments(totals.grandTotal, toCents(cash), toCents(card), toCents(transfer)),
    [totals.grandTotal, cash, card, transfer],
  )
  const linesValid = cart.length > 0 && cart.every((line) => line.quantity > 0 && Math.round(line.quantity * 100) / 100 === line.quantity)
  const canComplete = report !== null && linesValid && !totals.discountTooLarge && plan.valid && !submitting

  // what is on the price list under the typed text; with nothing typed, the first products to pick from
  useEffect(() => {
    let ignore = false
    const handle = window.setTimeout(() => {
      setSearching(true)
      productsApi
        .list({ search: term.trim() || undefined, activeOnly: true, limit: 12 })
        .then((page) => {
          if (!ignore) setResults(page.items)
        })
        .catch(() => {
          if (!ignore) setResults([])
        })
        .finally(() => {
          if (!ignore) setSearching(false)
        })
    }, term.trim() === '' ? 0 : 250)
    return () => {
      ignore = true
      window.clearTimeout(handle)
    }
  }, [term, completed])

  // a change of the total makes the payments typed so far meaningless, so they start again
  const [paidFor, setPaidFor] = useState(totals.grandTotal)
  if (totals.grandTotal !== paidFor) {
    setPaidFor(totals.grandTotal)
    setCash(0)
    setCard(0)
    setTransfer(0)
  }

  const focusScan = () => window.setTimeout(() => scanRef.current?.focus(), 0)

  const addProduct = (product: Product) => {
    const existing = cart.find((line) => line.productId === product.id)
    const quantity = (existing?.quantity ?? 0) + 1
    if (product.tracksStock && quantity > (product.stockAvailable ?? 0)) {
      setNotice(t('common:sales.pos.stockLimit', { qty: product.stockAvailable ?? 0 }))
      return
    }
    setNotice('')
    setError('')
    if (existing) {
      setCart(cart.map((line) => (line === existing ? { ...line, quantity } : line)))
    } else {
      setCart([
        ...cart,
        {
          key: product.id,
          productId: product.id,
          code: product.code,
          name: product.name,
          unit: product.unit,
          quantity: 1,
          unitPrice: product.salePrice,
          vatRate: product.vatRate,
          discount: 0,
          tracksStock: product.tracksStock,
          stockAvailable: product.stockAvailable,
        },
      ])
    }
    setTerm('')
    focusScan()
  }

  // Enter, which is how a scanner ends its code: an exact barcode or code first, then the only match of the search
  const handleScan = async () => {
    const value = term.trim()
    if (!value) return
    try {
      addProduct(await productsApi.lookup(value))
    } catch (reason) {
      if (reason instanceof ApiError && reason.status === 404) {
        if (results.length === 1) addProduct(results[0])
        else setNotice(t('common:sales.pos.notFoundCode', { term: value }))
      } else {
        setError(errorText(reason))
      }
    }
  }

  const updateLine = (key: string, change: Partial<CartLine>) => setCart(cart.map((line) => (line.key === key ? { ...line, ...change } : line)))

  const setQuantity = (line: CartLine, quantity: number) => {
    if (line.tracksStock && quantity > (line.stockAvailable ?? 0)) {
      setNotice(t('common:sales.pos.stockLimit', { qty: line.stockAvailable ?? 0 }))
      updateLine(line.key, { quantity: line.stockAvailable ?? 0 })
      return
    }
    setNotice('')
    updateLine(line.key, { quantity })
  }

  const reset = () => {
    setCart([])
    setReceiptDiscount(0)
    setCustomer(null)
    setNote('')
    setCash(0)
    setCard(0)
    setTransfer(0)
    setReference('')
    setNotice('')
    setError('')
    setTerm('')
  }

  const complete = async () => {
    if (!canComplete) return
    setSubmitting(true)
    setError('')
    try {
      const payments: SalePaymentRequest[] = []
      if (cash > 0) payments.push({ method: 'Cash', amount: cash })
      if (card > 0) payments.push({ method: 'Card', amount: card, reference: reference.trim() || undefined })
      if (transfer > 0) payments.push({ method: 'BankTransfer', amount: transfer, reference: reference.trim() || undefined })
      const sale = await quickSalesApi.create({
        customerId: customer?.id ?? null,
        lines: cart.map((line) =>
          line.productId
            ? { productId: line.productId, quantity: line.quantity, discountAmount: line.discount }
            : { description: line.name, quantity: line.quantity, unitPrice: line.unitPrice, vatRate: line.vatRate, discountAmount: line.discount },
        ),
        receiptDiscount,
        payments,
        note: note.trim() || undefined,
      })
      setCompleted(sale)
      reset()
      onSold()
    } catch (reason) {
      setError(errorText(reason))
    } finally {
      setSubmitting(false)
    }
  }

  if (report === null) {
    return (
      <Paper sx={{ p: 4, textAlign: 'center' }}>
        <Typography variant="h6" gutterBottom>{t('common:sales.pos.needShift')}</Typography>
        <Button variant="contained" onClick={onOpenShift} data-testid="button-2e094e">{t('common:sales.pos.openShift')}</Button>
      </Paper>
    )
  }

  return (
    <Grid container spacing={2}>
      <Grid size={{ xs: 12, md: 5 }}>
        <Paper sx={{ p: 2 }}>
          <TextField
            fullWidth
            autoFocus
            inputRef={scanRef}
            value={term}
            onChange={(event) => setTerm(event.target.value)}
            onKeyDown={(event) => {
              if (event.key === 'Enter') {
                event.preventDefault()
                void handleScan()
              }
            }}
            placeholder={t('common:sales.pos.scanPlaceholder')}
            slotProps={{
              input: {
                startAdornment: <InputAdornment position="start"><QrCodeScannerIcon /></InputAdornment>,
                endAdornment: searching ? <CircularProgress size={18} /> : undefined,
              },
            }}
           data-testid="textfield-dc159a" />
          {notice && <Alert severity="warning" sx={{ mt: 1 }} onClose={() => setNotice('')}>{notice}</Alert>}

          <List dense sx={{ mt: 1, maxHeight: { md: 460 }, overflow: 'auto' }}>
            {results.map((product) => (
              <ListItemButton key={product.id} onClick={() => addProduct(product)} divider>
                <ListItemText
                  primary={product.name}
                  secondary={[product.code, product.barcode].filter(Boolean).join(' · ')}
                  slotProps={{ primary: { noWrap: true } }}
                />
                <Box sx={{ textAlign: 'right', ml: 1 }}>
                  <Typography variant="body2" sx={{ fontWeight: 600 }}>{formatMoney(product.salePrice, lang)}</Typography>
                  {product.tracksStock && (
                    <Chip
                      size="small"
                      variant="outlined"
                      color={(product.stockAvailable ?? 0) > 0 ? 'default' : 'warning'}
                      label={t('common:sales.pos.stockAvailable', { qty: product.stockAvailable ?? 0 })}
                    />
                  )}
                </Box>
              </ListItemButton>
            ))}
            {results.length === 0 && !searching && (
              <Typography color="text.secondary" sx={{ p: 2 }}>{t('common:sales.pos.noMatch')}</Typography>
            )}
          </List>

          <Button sx={{ mt: 1 }} startIcon={<AddIcon />} onClick={() => setFreeLineOpen(true)} data-testid="button-e1ea3e">{t('common:sales.pos.freeLine')}</Button>
        </Paper>
      </Grid>

      <Grid size={{ xs: 12, md: 7 }}>
        <Paper sx={{ p: 2 }}>
          <Typography variant="h6" gutterBottom>{t('common:sales.pos.cart')}</Typography>

          {cart.length === 0 ? (
            <Typography color="text.secondary" sx={{ py: 3, textAlign: 'center' }}>{t('common:sales.pos.emptyCart')}</Typography>
          ) : (
            <Stack divider={<Divider />} spacing={1.5}>
              {cart.map((line, index) => (
                <Box key={line.key}>
                  <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start', gap: 1 }}>
                    <Box sx={{ minWidth: 0 }}>
                      <Typography variant="body2" sx={{ fontWeight: 600 }}>{line.name}</Typography>
                      <Typography variant="caption" color="text.secondary">
                        {`${money(toCents(line.unitPrice))} / ${line.unit} · %${line.vatRate}`}
                      </Typography>
                    </Box>
                    <IconButton size="small" title={t('common:sales.pos.remove')} onClick={() => setCart(cart.filter((item) => item.key !== line.key))} data-testid="iconbutton-65a462">
                      <DeleteOutlineIcon fontSize="small" />
                    </IconButton>
                  </Box>
                  <Box sx={{ display: 'flex', alignItems: 'center', gap: 1, flexWrap: 'wrap', mt: 0.5 }}>
                    <Box sx={{ display: 'flex', alignItems: 'center' }}>
                      <IconButton size="small" onClick={() => setQuantity(line, Math.max(line.quantity - 1, 0))} data-testid="iconbutton-385923"><RemoveIcon fontSize="small" /></IconButton>
                      <AmountField
                        size="small"
                        value={line.quantity}
                        onChange={(quantity) => setQuantity(line, quantity)}
                        error={line.quantity <= 0}
                        sx={{ width: 64 }}
                        slotProps={{ htmlInput: { style: { textAlign: 'center' } } }}
                      />
                      <IconButton size="small" onClick={() => setQuantity(line, line.quantity + 1)} data-testid="iconbutton-47eb57"><AddIcon fontSize="small" /></IconButton>
                    </Box>
                    <AmountField
                      size="small"
                      label={t('common:sales.pos.discount')}
                      value={line.discount}
                      onChange={(discount) => updateLine(line.key, { discount })}
                      error={totals.lines[index].discount !== toCents(line.discount)}
                      sx={{ width: 104 }}
                      slotProps={{ htmlInput: { style: { textAlign: 'right' } } }}
                    />
                    <Box sx={{ flexGrow: 1 }} />
                    <Typography sx={{ fontWeight: 700, whiteSpace: 'nowrap' }}>{money(totals.lines[index].gross - totals.lines[index].discount)}</Typography>
                  </Box>
                </Box>
              ))}
            </Stack>
          )}

          {cart.length > 0 && (
            <>
              <Divider sx={{ my: 2 }} />
              <Stack spacing={1}>
                <CustomerPicker value={customer} onChange={setCustomer} label={t('common:sales.pos.customer')} />
                <TextField size="small" label={t('common:sales.pos.note')} value={note} onChange={(event) => setNote(event.target.value)}  data-testid="textfield-399f3e" />
              </Stack>

              <Box sx={{ mt: 2, display: 'grid', gridTemplateColumns: '1fr auto', rowGap: 0.5, columnGap: 2 }}>
                <Typography color="text.secondary">{t('common:sales.pos.subtotal')}</Typography>
                <Typography align="right">{money(totals.subtotal)}</Typography>
                {totals.lineDiscount > 0 && (
                  <>
                    <Typography color="text.secondary">{t('common:sales.pos.discounts')}</Typography>
                    <Typography align="right">{`−${money(totals.lineDiscount)}`}</Typography>
                  </>
                )}
                <Typography color="text.secondary" sx={{ alignSelf: 'center' }}>{t('common:sales.pos.receiptDiscount')}</Typography>
                <AmountField
                  size="small"
                  value={receiptDiscount}
                  onChange={setReceiptDiscount}
                  error={totals.discountTooLarge}
                  sx={{ width: 110, justifySelf: 'end' }}
                  slotProps={{ htmlInput: { style: { textAlign: 'right' } } }}
                />
              </Box>
              {totals.discountTooLarge && <Alert severity="error" sx={{ mt: 1 }}>{t('common:sales.pos.discountTooLarge')}</Alert>}

              <Box sx={{ mt: 1, display: 'flex', justifyContent: 'space-between', alignItems: 'baseline' }}>
                <Typography variant="h5">{t('common:sales.pos.total')}</Typography>
                <Box sx={{ textAlign: 'right' }}>
                  <Typography variant="h4" sx={{ fontWeight: 700 }}>{money(totals.grandTotal)}</Typography>
                  <Typography variant="caption" color="text.secondary">{t('common:sales.pos.vatIncluded', { amount: money(totals.vatTotal) })}</Typography>
                </Box>
              </Box>

              <Divider sx={{ my: 2 }} />
              <Typography variant="subtitle1" sx={{ fontWeight: 600 }} gutterBottom>{t('common:sales.pos.payment')}</Typography>
              <Stack direction="row" spacing={1} sx={{ mb: 1 }}>
                <Button size="small" variant="outlined" onClick={() => { setCash(fromCents(totals.grandTotal)); setCard(0); setTransfer(0) }} data-testid="button-495d2d">{t('common:sales.pos.allCash')}</Button>
                <Button size="small" variant="outlined" onClick={() => { setCash(0); setCard(fromCents(totals.grandTotal)); setTransfer(0) }} data-testid="button-5668e6">{t('common:sales.pos.allCard')}</Button>
              </Stack>
              <Grid container spacing={1}>
                <Grid size={{ xs: 12, sm: 4 }}>
                  <AmountField fullWidth size="small" label={t('common:sales.pos.cashReceived')} value={cash} onChange={setCash} />
                </Grid>
                <Grid size={{ xs: 6, sm: 4 }}>
                  <AmountField fullWidth size="small" label={t('common:sales.pos.cardAmount')} value={card} onChange={setCard} />
                </Grid>
                <Grid size={{ xs: 6, sm: 4 }}>
                  <AmountField fullWidth size="small" label={t('common:sales.pos.transferAmount')} value={transfer} onChange={setTransfer} />
                </Grid>
                {(card > 0 || transfer > 0) && (
                  <Grid size={12}>
                    <TextField fullWidth size="small" label={t('common:sales.pos.reference')} value={reference} onChange={(event) => setReference(event.target.value)}  data-testid="textfield-51c5e9" />
                  </Grid>
                )}
              </Grid>

              <Box sx={{ mt: 1, display: 'flex', justifyContent: 'space-between' }}>
                {plan.overCard ? (
                  <Typography color="error">{t('common:sales.pos.overCard')}</Typography>
                ) : plan.remaining > 0 ? (
                  <Typography color="warning.main">{`${t('common:sales.pos.remaining')}: ${money(plan.remaining)}`}</Typography>
                ) : (
                  <span />
                )}
                {plan.change > 0 && <Typography color="success.main" sx={{ fontWeight: 700 }}>{`${t('common:sales.pos.change')}: ${money(plan.change)}`}</Typography>}
              </Box>

              {error && <Alert severity="error" sx={{ mt: 1 }}>{error}</Alert>}

              <Box sx={{ mt: 2, display: 'flex', gap: 1 }}>
                <Button color="inherit" onClick={reset} disabled={submitting} data-testid="button-052653">{t('common:sales.pos.clear')}</Button>
                <Button fullWidth size="large" variant="contained" disabled={!canComplete} onClick={() => void complete()} data-testid="button-b437ad">
                  {submitting ? <CircularProgress size={24} /> : `${t('common:sales.pos.complete')} · ${money(totals.grandTotal)}`}
                </Button>
              </Box>
            </>
          )}
        </Paper>
      </Grid>

      <FreeLineDialog
        open={freeLineOpen}
        onClose={() => setFreeLineOpen(false)}
        onAdd={(line) => {
          setCart([...cart, line])
          setFreeLineOpen(false)
          focusScan()
        }}
      />

      <Dialog open={completed !== null} onClose={() => { setCompleted(null); focusScan() }} maxWidth="xs" fullWidth data-testid="dialog-8d1d8d">
        <DialogTitle>{t('common:sales.pos.saleDone')}</DialogTitle>
        <DialogContent>{completed && <Receipt sale={completed} />}</DialogContent>
        <DialogActions>
          <Button startIcon={<PrintIcon />} onClick={() => window.print()} data-testid="button-e4107f">{t('common:sales.pos.print')}</Button>
          <Button variant="contained" onClick={() => { setCompleted(null); focusScan() }} data-testid="button-97b381">{t('common:sales.pos.newSale')}</Button>
        </DialogActions>
      </Dialog>
    </Grid>
  )
}

function FreeLineDialog({ open, onClose, onAdd }: { open: boolean; onClose: () => void; onAdd: (line: CartLine) => void }) {
  const { translate: t } = useI18n()
  const [description, setDescription] = useState('')
  const [price, setPrice] = useState(0)
  const [vatRate, setVatRate] = useState(20)
  const [quantity, setQuantity] = useState(1)
  const valid = description.trim() !== '' && price > 0 && quantity > 0 && Math.round(quantity * 100) / 100 === quantity

  const add = () => {
    onAdd({
      key: crypto.randomUUID(),
      productId: null,
      name: description.trim(),
      unit: 'adet',
      quantity,
      unitPrice: price,
      vatRate,
      discount: 0,
      tracksStock: false,
    })
    setDescription('')
    setPrice(0)
    setQuantity(1)
  }

  return (
    <Dialog open={open} onClose={onClose} maxWidth="xs" fullWidth data-testid="dialog-bac4a4">
      <DialogTitle>{t('common:sales.free.title')}</DialogTitle>
      <DialogContent>
        <Stack spacing={2} sx={{ mt: 1 }}>
          <Typography variant="body2" color="text.secondary">{t('common:sales.free.hint')}</Typography>
          <TextField autoFocus label={t('common:sales.free.description')} value={description} onChange={(event) => setDescription(event.target.value)}  data-testid="textfield-fdc6f7" />
          <AmountField label={t('common:sales.free.unitPrice')} value={price} onChange={setPrice} />
          <TextField select label={t('common:sales.free.vatRate')} value={vatRate} onChange={(event) => setVatRate(Number(event.target.value))} data-testid="textfield-3df52b">
            {VAT_RATES.map((rate) => <MenuItem key={rate} value={rate} data-testid="menuitem-8cd387">{`%${rate}`}</MenuItem>)}
          </TextField>
          <AmountField label={t('common:sales.free.quantity')} value={quantity} onChange={setQuantity} />
        </Stack>
      </DialogContent>
      <DialogActions>
        <Button onClick={onClose} data-testid="button-05295e">{t('common:actions.cancel')}</Button>
        <Button variant="contained" disabled={!valid} onClick={add} data-testid="button-0cbf08">{t('common:sales.free.add')}</Button>
      </DialogActions>
    </Dialog>
  )
}

import { useState, useMemo } from 'react'
import { Box, Typography, Paper, Stack, Button, TextField, IconButton, Select, MenuItem, FormControl, InputLabel, Dialog, DialogTitle, DialogContent, List, ListItemButton, ListItemText, Chip, Avatar, Zoom, Tabs, Tab, InputAdornment, Drawer, Divider, Snackbar, Alert } from '@mui/material'
import DeleteIcon from '@mui/icons-material/Delete'
import PrintIcon from '@mui/icons-material/Print'
import AddShoppingCartIcon from '@mui/icons-material/AddShoppingCart'
import CloseIcon from '@mui/icons-material/Close'
import ReceiptLongIcon from '@mui/icons-material/ReceiptLong'
import InventoryIcon from '@mui/icons-material/Inventory'
import SearchIcon from '@mui/icons-material/Search'
import SortIcon from '@mui/icons-material/Sort'
import FilterListIcon from '@mui/icons-material/FilterList'
import VisibilityIcon from '@mui/icons-material/Visibility'
import { PrintPreviewDialog, type PrintData } from '../../components/print/PrintPreviewDialog'
import { salesApi, type PaymentMethod } from '../../api/sales'

// Mock Data
const MOCK_CATEGORIES = ['Tümü', 'Kablolar', 'Şalterler', 'Aydınlatma', 'Priz & Anahtar', 'Sigortalar', 'Panolar', 'El Aletleri']
const MOCK_PRODUCTS = [
  { id: '1', code: 'KBL-01', category: 'Kablolar', name: 'NYA Kablo 2.5mm', price: 15.5, stock: 120, unit: 'm' },
  { id: '2', code: 'KBL-02', category: 'Kablolar', name: 'NYM Kablo 3x2.5mm', price: 45.0, stock: 80, unit: 'm' },
  { id: '3', code: 'SLT-01', category: 'Şalterler', name: 'Kompakt Şalter 160A', price: 1250.0, stock: 15, unit: 'Adet' },
  { id: '4', code: 'AYD-01', category: 'Aydınlatma', name: 'LED Panel 60x60', price: 350.0, stock: 42, unit: 'Adet' },
  { id: '5', code: 'PRZ-01', category: 'Priz & Anahtar', name: 'Sıva Altı Topraklı Priz', price: 65.0, stock: 200, unit: 'Adet' },
  { id: '6', code: 'SGR-01', category: 'Sigortalar', name: 'C Tipi Otomatik Sigorta 16A', price: 85.0, stock: 150, unit: 'Adet' },
  { id: '7', code: 'SGR-02', category: 'Sigortalar', name: 'C Tipi Otomatik Sigorta 32A', price: 95.0, stock: 8, unit: 'Adet' },
]

const MOCK_PAST_SALES = [
  { id: 'QS-1098', time: '10:45', total: 450.0, items: 3 },
  { id: 'QS-1097', time: '09:30', total: 1250.0, items: 1 },
  { id: 'QS-1096', time: '09:15', total: 65.0, items: 1 },
]

type QuickSaleLineInput = { productId?: string; productCode?: string; description: string; unit: string; quantity: number; unitPrice: number; vatRate: number; discountAmount: number; tracksStock: boolean }

export function QuickSaleView() {
  const [activeCategory, setActiveCategory] = useState('Tümü')
  const [searchQuery, setSearchQuery] = useState('')
  const [sortBy, setSortBy] = useState('name_asc')
  
  const [selectedProduct, setSelectedProduct] = useState<typeof MOCK_PRODUCTS[0] | null>(null)
  const [addQuantity, setAddQuantity] = useState(1)
  
  const [lines, setLines] = useState<QuickSaleLineInput[]>([])
  const [paymentMethod, setPaymentMethod] = useState<PaymentMethod>('Cash')
  const [customerName, setCustomerName] = useState('')
  const [loading, setLoading] = useState(false)
  const [printDialogOpen, setPrintDialogOpen] = useState(false)
  const [completedSaleNumber, setCompletedSaleNumber] = useState('')
  const [completedPrintData, setCompletedPrintData] = useState<PrintData | null>(null)
  const [showPrintPreview, setShowPrintPreview] = useState(false)
  const [pastSalesOpen, setPastSalesOpen] = useState(false)
  const [errorMsg, setErrorMsg] = useState<string | null>(null)

  const subtotal = lines.reduce((acc, l) => acc + (l.quantity * l.unitPrice), 0)
  const total = subtotal

  const filteredProducts = useMemo(() => {
    let result = MOCK_PRODUCTS.filter(p => 
      (activeCategory === 'Tümü' || p.category === activeCategory) &&
      (p.name.toLowerCase().includes(searchQuery.toLowerCase()) || p.code.toLowerCase().includes(searchQuery.toLowerCase()))
    )
    
    // Sort
    result = result.sort((a, b) => {
      if (sortBy === 'name_asc') return a.name.localeCompare(b.name)
      if (sortBy === 'price_asc') return a.price - b.price
      if (sortBy === 'price_desc') return b.price - a.price
      if (sortBy === 'stock_asc') return a.stock - b.stock
      return 0
    })

    return result
  }, [activeCategory, searchQuery, sortBy])

  const handleOpenDetail = (product: typeof MOCK_PRODUCTS[0]) => {
    setSelectedProduct(product)
    setAddQuantity(1)
  }

  const handleQuickAdd = (product: typeof MOCK_PRODUCTS[0], e: React.MouseEvent) => {
    e.stopPropagation()
    addToCart(product, 1)
  }

  const addToCart = (product: typeof MOCK_PRODUCTS[0], qty: number) => {
    const existing = lines.findIndex(l => l.productId === product.id)
    if (existing >= 0) {
      const newLines = [...lines]
      newLines[existing].quantity += qty
      setLines(newLines)
    } else {
      setLines([...lines, {
        productId: product.id,
        productCode: product.code,
        description: product.name,
        unit: product.unit,
        quantity: qty,
        unitPrice: product.price,
        vatRate: 20,
        discountAmount: 0,
        tracksStock: true
      }])
    }
  }

  const handleModalAddToCart = () => {
    if (!selectedProduct) return
    addToCart(selectedProduct, addQuantity)
    setSelectedProduct(null)
  }

  const handleRemoveLine = (index: number) => {
    setLines(lines.filter((_, i) => i !== index))
  }

  const handleComplete = async () => {
    if (lines.length === 0) return
    setLoading(true)
    try {
      const data = await salesApi.createQuick({
        shiftId: "00000000-0000-0000-0000-000000000000",
        customerId: null,
        receiptDiscount: 0,
        note: customerName ? `Customer: ${customerName}` : 'Walk-in',
        lines: lines,
        payments: [{ method: paymentMethod, amount: total, reference: undefined }]
      })
      
      const saleNo = data.saleNumber || 'QS-1234'
      setCompletedSaleNumber(saleNo)
      
      setCompletedPrintData({
        documentNo: saleNo,
        date: new Date().toLocaleDateString(),
        customerName: customerName || 'Perakende Müşteri (Walk-in)',
        items: lines.map(l => ({
          description: l.description,
          quantity: l.quantity,
          unit: l.unit,
          unitPrice: l.unitPrice,
          lineTotal: l.quantity * l.unitPrice
        })),
        subTotal: subtotal,
        taxTotal: subtotal * 0.2, // mock tax
        grandTotal: total * 1.2
      })
      
      setPrintDialogOpen(true)
      
      setLines([])
      setCustomerName('')
    } catch (e) {
      console.error(e)
      setErrorMsg("Satış işlemi sırasında bir hata oluştu.")
    } finally {
      setLoading(false)
    }
  }

  return (
    <Box sx={{ 
      display: 'flex', 
      height: { xs: 'calc(100vh - 56px - 72px)', sm: '100vh' }, 
      gap: 0, 
      m: -3, 
      mt: { xs: -10, sm: -3 }, 
      mb: { xs: -9, sm: -3 }, 
      bgcolor: 'background.paper' 
    }}>
      
      {/* LEFT COLUMN: Catalog (Search + Horizontal Categories + Grid) */}
      <Box sx={{ flex: 1, display: 'flex', flexDirection: 'column', overflow: 'hidden', gap: 0, borderRight: '2px solid', borderColor: 'divider' }}>
        
        {/* Search & Sort Row */}
        <Box sx={{ p: 1.5, borderBottom: '2px solid', borderColor: 'divider', display: 'flex', gap: 1.5, alignItems: 'center', bgcolor: 'background.paper' }}>
          <TextField 
            fullWidth 
            placeholder="Ürün adı veya kodu ile ara..." 
            variant="outlined" 
            size="small"
            value={searchQuery}
            onChange={(e) => setSearchQuery(e.target.value)}
            slotProps={{
              input: {
                startAdornment: <InputAdornment position="start"><SearchIcon /></InputAdornment>
              }
            }}
            sx={{ '& .MuiOutlinedInput-root': { borderRadius: 2, bgcolor: 'background.paper' } }}
          />
          <FormControl size="small" sx={{ minWidth: 200 }}>
            <Select 
              value={sortBy} 
              onChange={(e) => setSortBy(e.target.value)}
              startAdornment={<SortIcon sx={{ ml: 1, mr: 1, color: 'text.secondary' }} />}
              sx={{ borderRadius: 2 }}
            >
              <MenuItem value="name_asc">A-Z İsim</MenuItem>
              <MenuItem value="price_asc">Fiyat (Düşükten Yükseğe)</MenuItem>
              <MenuItem value="price_desc">Fiyat (Yüksekten Düşüğe)</MenuItem>
              <MenuItem value="stock_asc">Stok (Azdan Çoğa)</MenuItem>
            </Select>
          </FormControl>
          <Button variant="outlined" startIcon={<FilterListIcon />} sx={{ borderRadius: 2, whiteSpace: 'nowrap' }}>
            Filtrele
          </Button>
        </Box>
        
        {/* Categories Horizontal Tabs */}
        <Box sx={{ borderBottom: '2px solid', borderColor: 'divider', overflow: 'hidden', flexShrink: 0, bgcolor: 'background.paper' }}>
          <Tabs 
            value={activeCategory} 
            onChange={(_, val) => setActiveCategory(val)}
            variant="scrollable"
            scrollButtons="auto"
            sx={{ 
              minHeight: 48,
              '& .MuiTabs-indicator': { height: 3, borderRadius: '3px 3px 0 0' }
            }}
          >
            {MOCK_CATEGORIES.map(cat => (
              <Tab 
                key={cat} 
                value={cat} 
                label={cat} 
                sx={{ 
                  textTransform: 'none', 
                  fontWeight: activeCategory === cat ? 'bold' : 'medium',
                  fontSize: '0.9rem',
                  minHeight: 48
                }} 
              />
            ))}
          </Tabs>
        </Box>

        {/* Products Grid */}
        <Box sx={{ flex: 1, overflowY: 'auto', p: 1.5 }}>
          <Box sx={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fill, minmax(200px, 1fr))', gap: 1.5 }}>
            {filteredProducts.map(p => (
              <Zoom in key={p.id}>
                <Paper 
                  elevation={0}
                  sx={{ 
                    p: 1.5, 
                    borderRadius: 2, 
                    border: '1px solid', 
                    borderColor: 'divider',
                    position: 'relative',
                    bgcolor: 'background.paper',
                    display: 'flex',
                    flexDirection: 'column',
                    transition: 'all 0.2s',
                    '&:hover': {
                      borderColor: 'primary.main',
                      boxShadow: '0 4px 20px rgba(0,0,0,0.08)',
                      transform: 'translateY(-2px)'
                    }
                  }}
                >
                  <Chip 
                    size="small" 
                    label={`Stok: ${p.stock}`} 
                    color={p.stock < 10 ? 'error' : 'success'} 
                    sx={{ position: 'absolute', top: 12, right: 12, fontWeight: 'bold', fontSize: '0.7rem' }} 
                  />
                  <Avatar variant="rounded" sx={{ width: 48, height: 48, bgcolor: 'action.hover', color: 'text.secondary', mb: 1.5 }}>
                    <InventoryIcon />
                  </Avatar>
                  <Typography variant="body2" color="text.secondary" sx={{ fontSize: '0.75rem', mb: 0.5 }}>{p.code}</Typography>
                  <Typography variant="subtitle2" sx={{ fontWeight: 'bold', lineHeight: 1.2, mb: 0.5, minHeight: 38 }}>{p.name}</Typography>
                  <Typography variant="h6" color="primary.main" sx={{ fontWeight: 'bold', mb: 1.5, fontSize: '1.1rem' }}>₺{p.price.toFixed(2)}</Typography>
                  
                  <Stack direction="row" spacing={1} sx={{ mt: 'auto' }}>
                    <Button 
                      variant="contained" 
                      color="primary" 
                      fullWidth 
                      size="small"
                      onClick={(e) => handleQuickAdd(p, e)}
                      sx={{ borderRadius: 1.5, fontWeight: 'bold', py: 0.75 }}
                    >
                      Ekle
                    </Button>
                    <Button 
                      variant="outlined" 
                      size="small"
                      startIcon={<VisibilityIcon />}
                      onClick={() => handleOpenDetail(p)}
                      sx={{ borderRadius: 1.5, fontWeight: 'bold', py: 0.75, minWidth: '40%' }}
                    >
                      Detay
                    </Button>
                  </Stack>
                </Paper>
              </Zoom>
            ))}
          </Box>
        </Box>
      </Box>

      {/* RIGHT COLUMN: Cart */}
      <Box sx={{ width: 380, display: 'flex', flexDirection: 'column', gap: 0, flexShrink: 0, bgcolor: 'background.paper' }}>
        
        <Box sx={{ flex: 1, display: 'flex', flexDirection: 'column', overflow: 'hidden' }}>
          <Box sx={{ p: 1.5, bgcolor: 'primary.main', color: 'primary.contrastText', display: 'flex', justifyContent: 'space-between', alignItems: 'center', flexShrink: 0 }}>
            <Typography variant="subtitle1" sx={{ fontWeight: 'bold' }}>Sepet</Typography>
            <Box sx={{ display: 'flex', gap: 1, alignItems: 'center' }}>
              <Chip size="small" label={`${lines.length} Ürün`} sx={{ bgcolor: 'rgba(255,255,255,0.2)', color: 'inherit', fontWeight: 'bold' }} />
              <IconButton size="small" color="inherit" onClick={() => setPastSalesOpen(true)} title="Geçmiş Satışlar">
                <ReceiptLongIcon fontSize="small" />
              </IconButton>
            </Box>
          </Box>
          
          <Box sx={{ flex: 1, overflowY: 'auto', p: 1.5 }}>
            {lines.length === 0 ? (
              <Box sx={{ height: '100%', display: 'flex', alignItems: 'center', justifyContent: 'center', color: 'text.secondary' }}>
                <Typography variant="body2">Sepet henüz boş</Typography>
              </Box>
            ) : (
              <Stack spacing={1.5} divider={<Divider />}>
                {lines.map((l, i) => (
                  <Box key={i} sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                    <Box sx={{ flex: 1, overflow: 'hidden' }}>
                      <Typography variant="body2" noWrap sx={{ fontWeight: 'bold', maxWidth: 180 }}>{l.description}</Typography>
                      <Typography variant="caption" color="text.secondary">{l.quantity} {l.unit} x ₺{l.unitPrice}</Typography>
                    </Box>
                    <Typography variant="body2" sx={{ fontWeight: 'bold', mr: 2, flexShrink: 0 }}>₺{(l.quantity * l.unitPrice).toFixed(2)}</Typography>
                    <IconButton size="small" color="error" onClick={() => handleRemoveLine(i)} sx={{ flexShrink: 0 }}>
                      <DeleteIcon fontSize="small" />
                    </IconButton>
                  </Box>
                ))}
              </Stack>
            )}
          </Box>
          
          <Box sx={{ p: 1.5, borderTop: '2px solid', borderColor: 'divider', bgcolor: 'background.default', flexShrink: 0 }}>
            <Stack spacing={1} sx={{ mb: 1.5 }}>
              <TextField size="small" placeholder="Müşteri Adı (Opsiyonel / Walk-in)" value={customerName} onChange={e => setCustomerName(e.target.value)} fullWidth />
              <FormControl size="small" fullWidth>
                <InputLabel>Ödeme Yöntemi</InputLabel>
                <Select value={paymentMethod} label="Ödeme Yöntemi" onChange={e => setPaymentMethod(e.target.value)}>
                  <MenuItem value="Cash">Nakit</MenuItem>
                  <MenuItem value="Card">Kredi Kartı</MenuItem>
                  <MenuItem value="BankTransfer">Havale / EFT</MenuItem>
                </Select>
              </FormControl>
            </Stack>
            
            <Stack direction="row" sx={{ justifyContent: 'space-between', alignItems: 'center', mb: 1.5 }}>
              <Typography variant="subtitle1" sx={{ fontWeight: 'bold' }}>Toplam:</Typography>
              <Typography variant="h5" color="primary.main" sx={{ fontWeight: 'bold' }}>₺{total.toFixed(2)}</Typography>
            </Stack>
            
            <Button 
              variant="contained" 
              color="primary"
              size="medium" 
              fullWidth 
              onClick={handleComplete} 
              disabled={lines.length === 0 || loading}
              sx={{ py: 1, borderRadius: 2, fontWeight: 'bold' }}
            >
              {loading ? 'İşleniyor...' : 'Satışı Tamamla'}
            </Button>
          </Box>
        </Box>
      </Box>

      {/* Past Sales Drawer */}
      <Drawer anchor="right" open={pastSalesOpen} onClose={() => setPastSalesOpen(false)} sx={{ '& .MuiDrawer-paper': { width: 340 } }}>
        <Box sx={{ p: 2, display: 'flex', alignItems: 'center', justifyContent: 'space-between', borderBottom: '2px solid', borderColor: 'divider' }}>
          <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
            <ReceiptLongIcon color="action" />
            <Typography variant="subtitle1" sx={{ fontWeight: 'bold' }}>Geçmiş Satışlar (Bugün)</Typography>
          </Box>
          <IconButton size="small" onClick={() => setPastSalesOpen(false)}>
            <CloseIcon fontSize="small" />
          </IconButton>
        </Box>
        <List sx={{ p: 0 }}>
          {MOCK_PAST_SALES.map((sale, i) => (
            <ListItemButton key={sale.id} divider={i < MOCK_PAST_SALES.length - 1} sx={{ py: 1.5 }}>
              <ListItemText 
                primary={<Typography variant="body2" sx={{ fontWeight: 'bold' }}>{sale.id}</Typography>}
                secondary={`${sale.time} • ${sale.items} Ürün`} 
              />
              <Typography variant="body2" color="success.main" sx={{ fontWeight: 'bold' }}>₺{sale.total.toFixed(2)}</Typography>
            </ListItemButton>
          ))}
        </List>
      </Drawer>

      {/* Central Product Detail Modal (Dialog) */}
      <Dialog 
        open={!!selectedProduct} 
        onClose={() => setSelectedProduct(null)} 
        maxWidth="sm" 
        fullWidth
      >
        {selectedProduct && (
          <>
            <Box sx={{ p: 3, display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start', borderBottom: '2px solid', borderColor: 'divider' }}>
              <Box>
                <Typography variant="h5" sx={{ fontWeight: 'bold' }}>{selectedProduct.name}</Typography>
                <Typography variant="body2" color="text.secondary">{selectedProduct.code} • {selectedProduct.category}</Typography>
              </Box>
              <IconButton onClick={() => setSelectedProduct(null)}><CloseIcon /></IconButton>
            </Box>
            
            <DialogContent sx={{ p: 4 }}>
              <Box sx={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', gap: 4 }}>
                <Box>
                  <Typography variant="body2" color="text.secondary" gutterBottom>Birim Fiyat</Typography>
                  <Typography variant="h4" color="primary.main" sx={{ fontWeight: 'bold' }}>₺{selectedProduct.price.toFixed(2)}</Typography>
                  <Chip 
                    size="small" 
                    label={`Stokta: ${selectedProduct.stock} ${selectedProduct.unit}`} 
                    color={selectedProduct.stock < 10 ? 'error' : 'success'} 
                    sx={{ mt: 1, fontWeight: 'bold' }} 
                  />
                </Box>
                
                <Box sx={{ textAlign: 'center' }}>
                  <Typography variant="body2" color="text.secondary" gutterBottom>Miktar</Typography>
                  <Stack direction="row" spacing={2} sx={{ alignItems: 'center', bgcolor: 'background.default', p: 1, borderRadius: 2, border: '1px solid', borderColor: 'divider' }}>
                    <Button variant="contained" color="inherit" sx={{ minWidth: 40, p: 1, borderRadius: 1.5 }} onClick={() => setAddQuantity(Math.max(1, addQuantity - 1))}>-</Button>
                    <Typography variant="h5" sx={{ fontWeight: 'bold', width: 40, textAlign: 'center' }}>{addQuantity}</Typography>
                    <Button variant="contained" color="inherit" sx={{ minWidth: 40, p: 1, borderRadius: 1.5 }} onClick={() => setAddQuantity(addQuantity + 1)}>+</Button>
                  </Stack>
                </Box>
              </Box>
            </DialogContent>

            <Box sx={{ p: 3, borderTop: '2px solid', borderColor: 'divider', bgcolor: 'background.default' }}>
              <Button 
                variant="contained" 
                size="large" 
                fullWidth 
                startIcon={<AddShoppingCartIcon />}
                onClick={handleModalAddToCart}
                sx={{ py: 1.5, borderRadius: 2, fontSize: '1.1rem', fontWeight: 'bold' }}
              >
                Sepete Ekle - ₺{(selectedProduct.price * addQuantity).toFixed(2)}
              </Button>
            </Box>
          </>
        )}
      </Dialog>

      {/* Success / Print Dialog */}
      <Dialog open={printDialogOpen} onClose={() => setPrintDialogOpen(false)} maxWidth="xs" fullWidth>
        <DialogTitle sx={{ textAlign: 'center', fontWeight: 'bold', color: 'success.main', pt: 4 }}>Satış Başarılı!</DialogTitle>
        <DialogContent sx={{ textAlign: 'center', pb: 4 }}>
          <Typography variant="body1" sx={{ mb: 3 }}>
            {completedSaleNumber} numaralı satış işlemi başarıyla kaydedildi.
          </Typography>
          <Button variant="outlined" size="large" startIcon={<PrintIcon />} fullWidth onClick={() => setShowPrintPreview(true)} sx={{ mb: 2, py: 1.5, borderRadius: 2 }}>
            Fatura / Fiş Yazdır
          </Button>
          <Button onClick={() => setPrintDialogOpen(false)} fullWidth color="inherit" sx={{ py: 1 }}>
            Kapat & Yeni Satış
          </Button>
        </DialogContent>
      </Dialog>

      <PrintPreviewDialog 
        open={showPrintPreview} 
        onClose={() => setShowPrintPreview(false)} 
        type="Receipt" 
        data={completedPrintData} 
      />

      <Snackbar open={!!errorMsg} autoHideDuration={6000} onClose={() => setErrorMsg(null)}>
        <Alert onClose={() => setErrorMsg(null)} severity="error" sx={{ width: '100%' }}>
          {errorMsg}
        </Alert>
      </Snackbar>
    </Box>
  )
}

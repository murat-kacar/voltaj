import { useState } from 'react'
import { Box, Typography, Paper, TextField, Button, Stack, Table, TableHead, TableRow, TableCell, TableBody, IconButton, Divider, MenuItem } from '@mui/material'
import AddBoxIcon from '@mui/icons-material/AddBox'
import DeleteIcon from '@mui/icons-material/Delete'
import CloudUploadIcon from '@mui/icons-material/CloudUpload'
import InventoryIcon from '@mui/icons-material/Inventory'

const MOCK_PRODUCTS = [
  { id: '1', code: 'KBL-01', name: 'NYA Kablo 2.5mm', unit: 'm' },
  { id: '2', code: 'KBL-02', name: 'NYM Kablo 3x2.5mm', unit: 'm' },
  { id: '3', code: 'SLT-01', name: 'Kompakt Şalter 160A', unit: 'Adet' },
  { id: '4', code: 'AYD-01', name: 'LED Panel 60x60', unit: 'Adet' },
]

export function GoodsReceiptView() {
  const [supplier, setSupplier] = useState('')
  const [invoiceNo, setInvoiceNo] = useState('')
  const [invoiceDate, setInvoiceDate] = useState('')
  const [fileName, setFileName] = useState<string | null>(null)
  
  const [lines, setLines] = useState<{ id: string, qty: number, price: number }[]>([])

  const handleAddLine = () => {
    setLines([...lines, { id: MOCK_PRODUCTS[0].id, qty: 1, price: 0 }])
  }

  const handleUpdateLine = (index: number, field: string, value: any) => {
    const newLines = [...lines]
    newLines[index] = { ...newLines[index], [field]: value }
    setLines(newLines)
  }

  const handleRemoveLine = (index: number) => {
    setLines(lines.filter((_, i) => i !== index))
  }

  const handleFileUpload = (e: React.ChangeEvent<HTMLInputElement>) => {
    if (e.target.files && e.target.files.length > 0) {
      setFileName(e.target.files[0].name)
    }
  }

  const handleComplete = () => {
    if (lines.length === 0) {
      alert('Lütfen en az bir ürün ekleyin.')
      return
    }
    alert('Ürünler stoğa başarıyla işlendi ve fatura görseli sisteme kaydedildi!')
    setLines([])
    setSupplier('')
    setInvoiceNo('')
    setFileName(null)
  }

  const totalAmount = lines.reduce((acc, l) => acc + (l.qty * l.price), 0)

  return (
    <Box sx={{ maxWidth: 1200, mx: 'auto', p: { xs: 2, md: 4 } }}>
      <Stack direction="row" alignItems="center" spacing={2} sx={{ mb: 4 }}>
        <InventoryIcon color="primary" sx={{ fontSize: 32 }} />
        <Box>
          <Typography variant="h4" sx={{ fontWeight: 'bold' }}>Ürün Kabul (Mal Alım)</Typography>
          <Typography variant="body2" color="text.secondary">
            Tedarikçiden gelen ürünleri stoğa ekleyin ve alış faturasını yükleyin.
          </Typography>
        </Box>
      </Stack>

      <Stack direction={{ xs: 'column', md: 'row' }} spacing={4}>
        {/* Left Panel: Invoice Details & File Upload */}
        <Box sx={{ width: { xs: '100%', md: '35%' } }}>
          <Paper sx={{ p: 3, borderRadius: 2, mb: 3 }}>
            <Typography variant="h6" sx={{ fontWeight: 'bold', mb: 3 }}>Fatura ve Tedarikçi</Typography>
            <Stack spacing={3}>
              <TextField 
                label="Tedarikçi (Firma Adı)" 
                value={supplier} 
                onChange={e => setSupplier(e.target.value)} 
                fullWidth 
              />
              <TextField 
                label="Fatura Numarası" 
                value={invoiceNo} 
                onChange={e => setInvoiceNo(e.target.value)} 
                fullWidth 
              />
              <TextField 
                type="date"
                label="Fatura Tarihi" 
                value={invoiceDate} 
                onChange={e => setInvoiceDate(e.target.value)} 
                fullWidth 
                InputLabelProps={{ shrink: true }}
              />
              
              <Divider sx={{ my: 1 }} />
              
              <Box>
                <Typography variant="subtitle2" sx={{ mb: 1, fontWeight: 'bold', color: 'text.secondary' }}>Fatura Görseli (PDF/JPG)</Typography>
                <Button
                  component="label"
                  variant="outlined"
                  startIcon={<CloudUploadIcon />}
                  fullWidth
                  sx={{ py: 2, borderStyle: 'dashed', borderWidth: 2 }}
                >
                  {fileName ? fileName : 'Görsel Yükle'}
                  <input
                    type="file"
                    hidden
                    accept=".pdf,image/*"
                    onChange={handleFileUpload}
                  />
                </Button>
              </Box>
            </Stack>
          </Paper>
        </Box>

        {/* Right Panel: Products List */}
        <Box sx={{ width: { xs: '100%', md: '65%' } }}>
          <Paper sx={{ p: 3, borderRadius: 2 }}>
            <Stack direction="row" justifyContent="space-between" alignItems="center" sx={{ mb: 3 }}>
              <Typography variant="h6" sx={{ fontWeight: 'bold' }}>Gelen Ürünler</Typography>
              <Button variant="contained" color="secondary" startIcon={<AddBoxIcon />} onClick={handleAddLine}>
                Satır Ekle
              </Button>
            </Stack>

            {lines.length === 0 ? (
              <Box sx={{ py: 6, textAlign: 'center', bgcolor: 'action.hover', borderRadius: 2 }}>
                <Typography variant="body1" color="text.secondary">Henüz ürün eklenmedi. Stok girişi yapmak için "Satır Ekle"ye tıklayın.</Typography>
              </Box>
            ) : (
              <Box sx={{ overflowX: 'auto' }}>
                <Table size="small">
                  <TableHead>
                    <TableRow>
                      <TableCell sx={{ fontWeight: 'bold' }}>Ürün</TableCell>
                      <TableCell sx={{ fontWeight: 'bold', width: 100 }}>Miktar</TableCell>
                      <TableCell sx={{ fontWeight: 'bold', width: 150 }}>Birim Alış Fiyatı (₺)</TableCell>
                      <TableCell sx={{ fontWeight: 'bold' }} align="right">Toplam (₺)</TableCell>
                      <TableCell width={50}></TableCell>
                    </TableRow>
                  </TableHead>
                  <TableBody>
                    {lines.map((line, index) => {
                      const prod = MOCK_PRODUCTS.find(p => p.id === line.id)
                      const lineTotal = line.qty * line.price
                      return (
                        <TableRow key={index}>
                          <TableCell>
                            <TextField
                              select
                              size="small"
                              fullWidth
                              value={line.id}
                              onChange={e => handleUpdateLine(index, 'id', e.target.value)}
                            >
                              {MOCK_PRODUCTS.map(p => (
                                <MenuItem key={p.id} value={p.id}>{p.code} - {p.name}</MenuItem>
                              ))}
                            </TextField>
                          </TableCell>
                          <TableCell>
                            <TextField
                              type="number"
                              size="small"
                              value={line.qty}
                              onChange={e => handleUpdateLine(index, 'qty', parseFloat(e.target.value) || 0)}
                              InputProps={{ endAdornment: <Typography variant="caption" color="text.secondary" sx={{ ml: 1 }}>{prod?.unit}</Typography> }}
                            />
                          </TableCell>
                          <TableCell>
                            <TextField
                              type="number"
                              size="small"
                              fullWidth
                              value={line.price}
                              onChange={e => handleUpdateLine(index, 'price', parseFloat(e.target.value) || 0)}
                            />
                          </TableCell>
                          <TableCell align="right" sx={{ fontWeight: 'bold', verticalAlign: 'middle' }}>
                            {lineTotal.toFixed(2)}
                          </TableCell>
                          <TableCell>
                            <IconButton size="small" color="error" onClick={() => handleRemoveLine(index)}>
                              <DeleteIcon fontSize="small" />
                            </IconButton>
                          </TableCell>
                        </TableRow>
                      )
                    })}
                  </TableBody>
                </Table>
              </Box>
            )}

            <Divider sx={{ my: 3 }} />

            <Stack direction="row" justifyContent="space-between" alignItems="center">
              <Typography variant="h6">Fatura Ara Toplamı:</Typography>
              <Typography variant="h5" color="primary.main" sx={{ fontWeight: 'bold' }}>₺{totalAmount.toFixed(2)}</Typography>
            </Stack>

            <Button
              variant="contained"
              size="large"
              fullWidth
              disabled={lines.length === 0}
              onClick={handleComplete}
              sx={{ mt: 4, py: 1.5, fontSize: '1.1rem', fontWeight: 'bold' }}
            >
              Kaydet ve Stoğa Ekle
            </Button>
          </Paper>
        </Box>
      </Stack>
    </Box>
  )
}

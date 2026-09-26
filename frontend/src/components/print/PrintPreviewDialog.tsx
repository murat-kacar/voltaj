import { useRef } from 'react'
import { Dialog, DialogTitle, DialogContent, Button, Box, Typography, Stack, Divider, Table, TableHead, TableRow, TableCell, TableBody, Paper } from '@mui/material'
import PrintIcon from '@mui/icons-material/Print'
import DownloadIcon from '@mui/icons-material/Download'

export type PrintDocumentType = 'Invoice' | 'Quote' | 'Revision' | 'Receipt'

export interface PrintItem {
  description: string
  quantity: number
  unit: string
  unitPrice: number
  lineTotal: number
}

export interface PrintData {
  documentNo: string
  date: string
  dueDate?: string
  customerName: string
  customerDetails?: string
  items: PrintItem[]
  subTotal: number
  taxTotal: number
  grandTotal: number
  notes?: string
}

interface Props {
  open: boolean
  onClose: () => void
  type: PrintDocumentType
  data: PrintData | null
}

export function PrintPreviewDialog({ open, onClose, type, data }: Props) {
  const contentRef = useRef<HTMLDivElement>(null)

  const handlePrint = () => {
    window.print()
  }

  const getTitle = () => {
    switch (type) {
      case 'Invoice': return 'FATURA'
      case 'Quote': return 'TEKLİF FORMU'
      case 'Revision': return 'REVİZE TEKLİF'
      case 'Receipt': return 'SATIŞ FİŞİ'
      default: return 'BELGE'
    }
  }

  if (!data) return null

  const fmtCurrency = (val: number) => new Intl.NumberFormat('tr-TR', { style: 'currency', currency: 'TRY' }).format(val)

  return (
    <>
      <style>
        {`
          @media print {
            body * { visibility: hidden; }
            #printable-document, #printable-document * { visibility: visible; }
            #printable-document {
              position: absolute;
              left: 0;
              top: 0;
              width: 100%;
              padding: 20px;
              background-color: white !important;
              color: black !important;
            }
            @page { margin: 1cm; }
          }
        `}
      </style>
      <Dialog open={open} onClose={onClose} maxWidth="md" fullWidth slotProps={{ paper: { sx: { minHeight: '80vh', bgcolor: '#f5f5f5' } } }}>
        <DialogTitle sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', bgcolor: 'white', borderBottom: 1, borderColor: 'divider' }}>
          <Typography variant="h6" sx={{ fontWeight: 'bold' }}>Belge Önizleme ({getTitle()})</Typography>
          <Stack direction="row" spacing={1}>
            <Button variant="outlined" startIcon={<DownloadIcon />} disabled title="PDF İndir (Yakında)">PDF İndir</Button>
            <Button variant="contained" startIcon={<PrintIcon />} onClick={handlePrint}>Yazdır</Button>
            <Button onClick={onClose} color="inherit">Kapat</Button>
          </Stack>
        </DialogTitle>
        <DialogContent sx={{ p: 4, display: 'flex', justifyContent: 'center' }}>
          
          {/* A4 Paper Mockup Container */}
          <Paper 
            id="printable-document"
            ref={contentRef}
            elevation={3}
            sx={{ 
              width: '210mm', 
              minHeight: '297mm', 
              bgcolor: 'white', 
              p: '20mm', 
              boxSizing: 'border-box',
              display: 'flex',
              flexDirection: 'column'
            }}
          >
            {/* Header */}
            <Stack direction="row" sx={{ justifyContent: 'space-between', alignItems: 'flex-start', mb: 6 }}>
              <Box>
                <Typography variant="h4" sx={{ fontWeight: 900, color: 'primary.main', mb: 1, letterSpacing: -1 }}>VOLTFLOW</Typography>
                <Typography variant="body2" color="text.secondary">Voltflow Teknoloji A.Ş.</Typography>
                <Typography variant="body2" color="text.secondary">Teknopark İstanbul, 34906 Pendik/İstanbul</Typography>
                <Typography variant="body2" color="text.secondary">Tel: +90 216 123 45 67</Typography>
                <Typography variant="body2" color="text.secondary">VD: Pendik / 1234567890</Typography>
              </Box>
              <Box sx={{ textAlign: 'right' }}>
                <Typography variant="h3" sx={{ fontWeight: '300', color: 'text.disabled', textTransform: 'uppercase', letterSpacing: 2 }}>
                  {getTitle()}
                </Typography>
                <Box sx={{ mt: 2 }}>
                  <Typography variant="body2"><strong>Belge No:</strong> {data.documentNo}</Typography>
                  <Typography variant="body2"><strong>Tarih:</strong> {data.date}</Typography>
                  {data.dueDate && <Typography variant="body2"><strong>Geçerlilik/Vade:</strong> {data.dueDate}</Typography>}
                </Box>
              </Box>
            </Stack>

            <Divider sx={{ mb: 4, borderBottomWidth: 2 }} />

            {/* Customer Info */}
            <Box sx={{ mb: 6 }}>
              <Typography variant="overline" sx={{ color: 'text.secondary', fontWeight: 'bold' }}>Sayın / Müşteri Bilgileri</Typography>
              <Typography variant="h6" sx={{ fontWeight: 'bold' }}>{data.customerName}</Typography>
              {data.customerDetails && (
                <Typography variant="body2" sx={{ whiteSpace: 'pre-line', color: 'text.secondary', mt: 1 }}>
                  {data.customerDetails}
                </Typography>
              )}
            </Box>

            {/* Items Table */}
            <Table size="small" sx={{ mb: 4, '& th': { fontWeight: 'bold', bgcolor: 'action.hover', borderBottom: '2px solid rgba(224, 224, 224, 1)' } }}>
              <TableHead>
                <TableRow>
                  <TableCell>Açıklama / Ürün</TableCell>
                  <TableCell align="right">Miktar</TableCell>
                  <TableCell align="right">Birim Fiyat</TableCell>
                  <TableCell align="right">Toplam</TableCell>
                </TableRow>
              </TableHead>
              <TableBody>
                {data.items.map((item, idx) => (
                  <TableRow key={idx}>
                    <TableCell sx={{ py: 1.5 }}>
                      <Typography variant="body2" sx={{ fontWeight: 500 }}>{item.description}</Typography>
                    </TableCell>
                    <TableCell align="right">{item.quantity} {item.unit}</TableCell>
                    <TableCell align="right">{fmtCurrency(item.unitPrice)}</TableCell>
                    <TableCell align="right" sx={{ fontWeight: 'bold' }}>{fmtCurrency(item.lineTotal)}</TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>

            {/* Totals Section */}
            <Box sx={{ display: 'flex', justifyContent: 'flex-end', mb: 6 }}>
              <Box sx={{ width: '300px' }}>
                <Stack direction="row" sx={{ justifyContent: 'space-between', mb: 1 }}>
                  <Typography variant="body2" color="text.secondary">Ara Toplam:</Typography>
                  <Typography variant="body2" sx={{ fontWeight: 'bold' }}>{fmtCurrency(data.subTotal)}</Typography>
                </Stack>
                <Stack direction="row" sx={{ justifyContent: 'space-between', mb: 1 }}>
                  <Typography variant="body2" color="text.secondary">KDV (%20):</Typography>
                  <Typography variant="body2" sx={{ fontWeight: 'bold' }}>{fmtCurrency(data.taxTotal)}</Typography>
                </Stack>
                <Divider sx={{ my: 1, borderBottomWidth: 2 }} />
                <Stack direction="row" sx={{ justifyContent: 'space-between', alignItems: 'center' }}>
                  <Typography variant="subtitle1" sx={{ fontWeight: 'bold' }}>Genel Toplam:</Typography>
                  <Typography variant="h6" sx={{ fontWeight: 900, color: 'primary.main' }}>{fmtCurrency(data.grandTotal)}</Typography>
                </Stack>
              </Box>
            </Box>

            {/* Footer / Notes */}
            <Box sx={{ mt: 'auto', pt: 4, borderTop: '1px solid', borderColor: 'divider' }}>
              {data.notes && (
                <Box sx={{ mb: 4 }}>
                  <Typography variant="overline" sx={{ fontWeight: 'bold', color: 'text.secondary' }}>Notlar / Şartlar</Typography>
                  <Typography variant="body2" sx={{ whiteSpace: 'pre-line', fontSize: '0.8rem', color: 'text.secondary' }}>
                    {data.notes}
                  </Typography>
                </Box>
              )}
              
              {/* Signatures for Quotes */}
              {(type === 'Quote' || type === 'Revision') && (
                <Stack direction="row" sx={{ justifyContent: 'space-between', mt: 6, px: 4 }}>
                  <Box sx={{ textAlign: 'center' }}>
                    <Typography variant="body2" sx={{ fontWeight: 'bold', mb: 6 }}>Hazırlayan / Kaşe İmza</Typography>
                    <Divider sx={{ width: 150 }} />
                  </Box>
                  <Box sx={{ textAlign: 'center' }}>
                    <Typography variant="body2" sx={{ fontWeight: 'bold', mb: 6 }}>Müşteri Onayı / İmza</Typography>
                    <Divider sx={{ width: 150 }} />
                  </Box>
                </Stack>
              )}
            </Box>
          </Paper>
        </DialogContent>
      </Dialog>
    </>
  )
}

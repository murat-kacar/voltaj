import { Drawer, Box, Typography, IconButton, Divider, Chip } from '@mui/material'
import CloseIcon from '@mui/icons-material/Close'
import { useI18n, formatCurrency } from './i18n'
import type { Quote } from './api'

export function QuoteDetailDrawer({ quote, onClose }: { quote: Quote, onClose: () => void }) {
  const { translate: t, lang } = useI18n()

  return (
    <Drawer anchor="right" open={true} onClose={onClose} PaperProps={{ sx: { width: { xs: '100%', sm: 400 } } }}>
      <Box sx={{ p: 3, display: 'flex', alignItems: 'center', justifyContent: 'space-between' }}>
        <Typography variant="h6">{t('common:drawers.details')}</Typography>
        <IconButton onClick={onClose}><CloseIcon /></IconButton>
      </Box>
      <Divider />
      <Box sx={{ p: 3 }}>
        <Typography variant="overline" color="text.secondary">Quote Number</Typography>
        <Typography variant="h5" gutterBottom>{quote.number}</Typography>
        
        <Box sx={{ mt: 3 }}>
          <Typography variant="caption" color="text.secondary">Title</Typography>
          <Typography variant="body1" gutterBottom>{quote.title}</Typography>
        </Box>
        
        <Box sx={{ mt: 2 }}>
          <Typography variant="caption" color="text.secondary">Customer</Typography>
          <Typography variant="body1" gutterBottom>{quote.customerId}</Typography>
        </Box>
        
        <Box sx={{ mt: 2 }}>
          <Typography variant="caption" color="text.secondary">Status</Typography>
          <Box sx={{ mt: 0.5 }}>
            <Chip label={quote.status} color={quote.status === 'Sent' ? 'primary' : quote.status === 'Accepted' ? 'success' : 'default'} />
          </Box>
        </Box>
        
        <Box sx={{ mt: 2 }}>
          <Typography variant="caption" color="text.secondary">{t('common:fields.total')}</Typography>
          <Typography variant="h6" color="primary">{formatCurrency(quote.totalAmount, lang)}</Typography>
        </Box>
        
        <Box sx={{ mt: 4, pt: 3, borderTop: 1, borderColor: 'divider' }}>
          <Typography variant="caption" color="text.secondary">{t('common:drawers.createdBy')}</Typography>
          <Typography variant="body2">{quote.assignedUserId || 'System'}</Typography>
        </Box>
      </Box>
    </Drawer>
  )
}

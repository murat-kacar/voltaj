import { Drawer, Box, Typography, IconButton, Divider, Chip } from '@mui/material'
import CloseIcon from '@mui/icons-material/Close'
import { useI18n } from './i18n'
import { formatMoney } from './common/format'
import type { Quote } from './api'

export function QuoteDetailDrawer({ quote, onClose }: { quote: Quote, onClose: () => void }) {
  const { translate: t, lang } = useI18n()

  return (
    <Drawer anchor="right" open={true} onClose={onClose} sx={{ '& .MuiDrawer-paper': { width: { xs: '100%', sm: 400 } } }}>
      <Box sx={{ p: 3, display: 'flex', alignItems: 'center', justifyContent: 'space-between' }}>
        <Typography variant="h6">{t('common:drawers.details')}</Typography>
        <IconButton onClick={onClose}><CloseIcon /></IconButton>
      </Box>
      <Divider />
      <Box sx={{ p: 3 }}>
        <Typography variant="overline" color="text.secondary">{t('common:fields.quoteNumber')}</Typography>
        <Typography variant="h5" gutterBottom>{quote.number}</Typography>
        
        <Box sx={{ mt: 3 }}>
          <Typography variant="caption" color="text.secondary">{t('common:fields.title')}</Typography>
          <Typography variant="body1" gutterBottom>{quote.title}</Typography>
        </Box>
        
        <Box sx={{ mt: 2 }}>
          <Typography variant="caption" color="text.secondary">{t('common:fields.customer')}</Typography>
          <Typography variant="body1" gutterBottom>{quote.customerId}</Typography>
        </Box>
        
        <Box sx={{ mt: 2 }}>
          <Typography variant="caption" color="text.secondary">{t('common:fields.status')}</Typography>
          <Box sx={{ mt: 0.5 }}>
            <Chip label={quote.state} color={quote.state === 'Sent' ? 'primary' : quote.state === 'Accepted' ? 'success' : 'default'} />
          </Box>
        </Box>
        
        <Box sx={{ mt: 2 }}>
          <Typography variant="caption" color="text.secondary">{t('common:fields.total')}</Typography>
          <Typography variant="h6" color="primary">{formatMoney(quote.total, lang)}</Typography>
        </Box>
      </Box>
    </Drawer>
  )
}

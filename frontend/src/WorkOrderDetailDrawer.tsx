import { Drawer, Box, Typography, IconButton, Divider, Chip } from '@mui/material'
import CloseIcon from '@mui/icons-material/Close'
import { useI18n } from './i18n'
import type { WorkOrder } from './api'

export function WorkOrderDetailDrawer({ order, onClose }: { order: WorkOrder, onClose: () => void }) {
  const { translate: t } = useI18n()

  return (
    <Drawer anchor="right" open={true} onClose={onClose} sx={{ '& .MuiDrawer-paper': { width: { xs: '100%', sm: 400 } } }}>
      <Box sx={{ p: 3, display: 'flex', alignItems: 'center', justifyContent: 'space-between' }}>
        <Typography variant="h6">{t('common:drawers.details')}</Typography>
        <IconButton onClick={onClose}><CloseIcon /></IconButton>
      </Box>
      <Divider />
      <Box sx={{ p: 3 }}>
        <Typography variant="overline" color="text.secondary">{t('common:fields.workOrderNumber')}</Typography>
        <Typography variant="h5" gutterBottom>{order.number}</Typography>
        
        <Box sx={{ mt: 3 }}>
          <Typography variant="caption" color="text.secondary">{t('common:fields.title')}</Typography>
          <Typography variant="body1" gutterBottom>{order.title}</Typography>
        </Box>
        
        <Box sx={{ mt: 2 }}>
          <Typography variant="caption" color="text.secondary">{t('common:fields.customer')}</Typography>
          <Typography variant="body1" gutterBottom>{order.customerId}</Typography>
        </Box>
        
        <Box sx={{ mt: 2 }}>
          <Typography variant="caption" color="text.secondary">{t('common:fields.status')}</Typography>
          <Box sx={{ mt: 0.5 }}>
            <Chip label={order.status} color={order.status === 'Completed' ? 'success' : order.status === 'In Progress' ? 'warning' : 'default'} />
          </Box>
        </Box>
        
        <Box sx={{ mt: 4, pt: 3, borderTop: 1, borderColor: 'divider' }}>
          <Typography variant="caption" color="text.secondary">{t('common:drawers.createdBy')}</Typography>
          <Typography variant="body2">{order.assignedUserId || 'System'}</Typography>
        </Box>
      </Box>
    </Drawer>
  )
}

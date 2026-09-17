import { Drawer, Box, Typography, IconButton, Divider, Chip } from '@mui/material'
import CloseIcon from '@mui/icons-material/Close'
import { useI18n } from './i18n'
import type { WorkOrder } from './api'

export function WorkOrderDetailDrawer({ order, onClose }: { order: WorkOrder, onClose: () => void }) {
  const { translate: t } = useI18n()

  return (
    <Drawer anchor="right" open={true} onClose={onClose} PaperProps={{ sx: { width: { xs: '100%', sm: 400 } } }}>
      <Box sx={{ p: 3, display: 'flex', alignItems: 'center', justifyContent: 'space-between' }}>
        <Typography variant="h6">{t('common:drawers.details')}</Typography>
        <IconButton onClick={onClose}><CloseIcon /></IconButton>
      </Box>
      <Divider />
      <Box sx={{ p: 3 }}>
        <Typography variant="overline" color="text.secondary">Work Order Number</Typography>
        <Typography variant="h5" gutterBottom>{order.number}</Typography>
        
        <Box sx={{ mt: 3 }}>
          <Typography variant="caption" color="text.secondary">Title</Typography>
          <Typography variant="body1" gutterBottom>{order.title}</Typography>
        </Box>
        
        <Box sx={{ mt: 2 }}>
          <Typography variant="caption" color="text.secondary">Customer</Typography>
          <Typography variant="body1" gutterBottom>{order.customerId}</Typography>
        </Box>
        
        <Box sx={{ mt: 2 }}>
          <Typography variant="caption" color="text.secondary">Status</Typography>
          <Box sx={{ mt: 0.5 }}>
            <Chip label={order.status} color={order.status === 'Completed' ? 'success' : order.status === 'In Progress' ? 'warning' : 'default'} />
          </Box>
        </Box>
        
        <Box sx={{ mt: 2 }}>
          <Typography variant="caption" color="text.secondary">Description</Typography>
          <Typography variant="body2" sx={{ mt: 1 }}>{order.description || 'No description provided.'}</Typography>
        </Box>
        
        <Box sx={{ mt: 4, pt: 3, borderTop: 1, borderColor: 'divider' }}>
          <Typography variant="caption" color="text.secondary">{t('common:drawers.createdBy')}</Typography>
          <Typography variant="body2">{order.assignedUserId || 'System'}</Typography>
        </Box>
      </Box>
    </Drawer>
  )
}

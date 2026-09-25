import { useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { Box, Typography, Grid, Paper, Button, Dialog, DialogTitle, DialogContent } from '@mui/material'
import AutoFixHighIcon from '@mui/icons-material/AutoFixHigh'

import { useI18n } from '../i18n'
import { CustomerFormDialog } from '../customers/CustomerFormDialog'

export function DashboardView() {
  const { translate: t } = useI18n()
  const navigate = useNavigate()

  const [showQuickCreate, setShowQuickCreate] = useState(false)
  const [quickAction, setQuickAction] = useState<'customer' | null>(null)

  const navTo = (path: string) => navigate(`/${path}`)

  return (
    <Box sx={{ display: 'flex', flexDirection: { xs: 'column', md: 'row' }, gap: 3 }}>
      <Box sx={{ flex: 1, minWidth: 0 }}>
        <Box sx={{ display: 'flex', justifyContent: 'space-between', mb: 3 }}>
          <Typography variant="h4">{t('common:nav.overview')}</Typography>
        </Box>
        <Grid container spacing={2}>
          <Grid size={{ xs: 12, sm: 6 }}>
            <Paper sx={{ p: 3, borderTop: '4px solid', borderColor: 'primary.main' }}>
              <Typography color="text.secondary" gutterBottom>Customers</Typography>
              <Typography variant="h3">0</Typography>
            </Paper>
          </Grid>
          <Grid size={{ xs: 12, sm: 6 }}>
            <Paper sx={{ p: 3, borderTop: '4px solid', borderColor: 'secondary.main' }}>
              <Typography color="text.secondary" gutterBottom>Products</Typography>
              <Typography variant="h3">0</Typography>
            </Paper>
          </Grid>
        </Grid>
      </Box>
      <Box sx={{ width: { xs: '100%', md: 300 }, flexShrink: 0 }}>
        <Typography variant="h6" gutterBottom>{t('common:dashboard.quickActions')}</Typography>
        <Button variant="contained" fullWidth sx={{ mb: 2 }} onClick={() => setShowQuickCreate(true)} data-testid="button-aad08d">
          + {t('common:actions.create')}
        </Button>
        <Button
          variant="contained"
          color="secondary"
          fullWidth
          sx={{ mb: 2 }}
          onClick={() => navTo('pos')}
        >
          POS / Hızlı Satış
        </Button>
        <Button
          variant="outlined"
          color="primary"
          fullWidth
          sx={{ mb: 2 }}
          startIcon={<AutoFixHighIcon />}
          onClick={() => navTo('test-data')}
          data-testid="dashboard-btn-test-data"
        >
          {t('common:generator.title')}
        </Button>
      </Box>

      <Dialog open={showQuickCreate} onClose={() => setShowQuickCreate(false)} maxWidth="xs" fullWidth data-testid="dialog-36c522">
        <DialogTitle>{t('common:dashboard.quickActions')}</DialogTitle>
        <DialogContent>
          <Box sx={{ display: 'flex', flexDirection: 'column', gap: 2, mt: 1 }}>
            <Button variant="outlined" onClick={() => { setQuickAction('customer'); setShowQuickCreate(false) }} data-testid="button-3133bc">{t('common:actions.createCustomer')}</Button>
          </Box>
        </DialogContent>
      </Dialog>

      {quickAction === 'customer' && <CustomerFormDialog customer={null} onClose={() => setQuickAction(null)} onSaved={() => setQuickAction(null)} />}
    </Box>
  )
}

import { useNavigate } from 'react-router-dom'
import { Box, Typography, Grid, Button } from '@mui/material'
import AutoFixHighIcon from '@mui/icons-material/AutoFixHigh'

import { useI18n } from '../../i18n'
import { RemindersView } from './reminders/RemindersView'

export function AgendaView() {
  const { translate: t } = useI18n()
  const navigate = useNavigate()



  const navTo = (path: string) => navigate(`/${path}`)

  return (
    <Box sx={{ display: 'flex', flexDirection: { xs: 'column', md: 'row' }, gap: 3 }}>
      <Box sx={{ flex: 1, minWidth: 0 }}>
        <Box sx={{ display: 'flex', justifyContent: 'space-between', mb: 3 }}>
          <Typography variant="h4">{t('common:nav.overview')}</Typography>
        </Box>
        <Grid container spacing={2}>
          <Grid size={{ xs: 12 }}>
            <RemindersView />
          </Grid>
        </Grid>
      </Box>
      <Box sx={{ width: { xs: '100%', md: 300 }, flexShrink: 0 }}>
        <Typography variant="h6" gutterBottom>{t('common:dashboard.quickActions')}</Typography>

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


    </Box>
  )
}

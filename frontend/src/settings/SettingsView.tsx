import { Box, Typography, Button, Paper } from '@mui/material'
import AutoFixHighIcon from '@mui/icons-material/AutoFixHigh'
import { useNavigate } from 'react-router-dom'
import { AuditLogPanel } from '../OperationsViews'
import { PageHeader } from '../common/PageHeader'
import { useI18n } from '../i18n'

/** The shop's system settings and audit trail. */
export function SettingsView() {
  const { translate: t } = useI18n()
  const navigate = useNavigate()

  return (
    <Box>
      <PageHeader overline={t('common:nav.system')} title={t('common:nav.settings')} />

      <Paper variant="outlined" sx={{ p: 2.5, mt: 3, display: 'flex', alignItems: 'center', justifyContent: 'space-between', flexWrap: 'wrap', gap: 2 }}>
        <Box>
          <Typography variant="subtitle1" sx={{ fontWeight: 600 }}>
            {t('common:nav.testData')}
          </Typography>
          <Typography variant="body2" color="text.secondary">
            {t('common:generator.settingsSubtitle')}
          </Typography>
        </Box>
        <Button
          variant="contained"
          startIcon={<AutoFixHighIcon />}
          onClick={() => navigate('/test-data')}
          data-testid="settings-goto-test-data"
        >
          {t('common:nav.testData')} {t('common:generator.goToGenerator')}
        </Button>
      </Paper>

      <Box component="section" sx={{ mt: 4 }}>
        <Typography variant="h6" component="h3" gutterBottom>{t('common:nav.auditLog')}</Typography>
        <AuditLogPanel />
      </Box>
    </Box>
  )
}

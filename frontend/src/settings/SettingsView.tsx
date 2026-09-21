import { Box, Typography } from '@mui/material'
import { AuditLogPanel } from '../OperationsViews'
import { PageHeader } from '../common/PageHeader'
import { useI18n } from '../i18n'

/** The shop's system settings and audit trail. */
export function SettingsView() {
  const { translate: t } = useI18n()

  return (
    <Box>
      <PageHeader overline={t('common:nav.system')} title={t('common:nav.settings')} />
      <Box component="section" sx={{ mt: 3 }}>
        <Typography variant="h6" component="h3" gutterBottom>{t('common:nav.auditLog')}</Typography>
        <AuditLogPanel />
      </Box>
    </Box>
  )
}

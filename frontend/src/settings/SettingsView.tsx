import { Box, Divider, Stack, Typography } from '@mui/material'
import { AuditLogPanel } from '../OperationsViews'
import { PageHeader } from '../common/PageHeader'
import { useI18n } from '../i18n'
import { UsersPanel } from './UsersPanel'

/** The shop's own setup: who may sign in and with which roles, and the record of what was done. */
export function SettingsView() {
  const { translate: t } = useI18n()

  return (
    <Box>
      <PageHeader overline={t('common:nav.system')} title={t('common:nav.settings')} />
      <Stack spacing={4} divider={<Divider flexItem />}>
        <Box component="section">
          <Typography variant="h6" component="h3" gutterBottom>{t('common:settings.users.title')}</Typography>
          <UsersPanel />
        </Box>
        <Box component="section">
          <Typography variant="h6" component="h3" gutterBottom>{t('common:nav.auditLog')}</Typography>
          <AuditLogPanel />
        </Box>
      </Stack>
    </Box>
  )
}

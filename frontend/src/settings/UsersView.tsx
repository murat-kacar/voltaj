import { Box } from '@mui/material'
import { PageHeader } from '../common/PageHeader'
import { useI18n } from '../i18n'
import { UsersPanel } from './UsersPanel'

/** Who may sign in: the users, the ones still waiting for approval, and the roles each holds. */
export function UsersView() {
  const { translate: t } = useI18n()

  return (
    <Box>
      <PageHeader overline={t('common:nav.system')} title={t('common:nav.users')} />
      <UsersPanel />
    </Box>
  )
}

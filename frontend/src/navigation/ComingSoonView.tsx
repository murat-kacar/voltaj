import { Alert, Box } from '@mui/material'
import { PageHeader } from '../common/PageHeader'
import { useI18n } from '../i18n'

/** Stands in for a module of the menu that has no screen yet. */
export function ComingSoonView({ title }: { title: string }) {
  const { translate: t } = useI18n()
  return (
    <Box data-testid="coming-soon">
      <PageHeader overline={t('common:nav.comingSoonTitle')} title={title} />
      <Alert severity="info">{t('common:nav.comingSoonText')}</Alert>
    </Box>
  )
}

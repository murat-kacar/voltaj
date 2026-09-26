import { useMemo } from 'react'
import { Box } from '@mui/material'
import { sessionRoles } from '../../api'
import { ProductsTab } from '../inventory/ProductsTab'
import { PageHeader } from '../../common/PageHeader'
import { useI18n } from '../../i18n'

/** The catalog: every item the shop sells or uses, with its price. What is on the shelf is Stock's business. */
export function CatalogView() {
  const { translate: t } = useI18n()
  const isManager = useMemo(() => sessionRoles().some((role) => role === 'Admin' || role === 'Manager'), [])

  return (
    <Box>
      <PageHeader overline={t('common:nav.catalogStock')} title={t('common:nav.catalog')} />
      <ProductsTab isManager={isManager} />
    </Box>
  )
}

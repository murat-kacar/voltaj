import { trTR } from '@mui/x-data-grid/locales'
import { ApiError } from './api'
import { i18n, translateApiError } from './i18n'

/** A message the user can read for whatever went wrong, in the language of the screen. */
export function errorText(reason: unknown): string {
  if (reason instanceof ApiError) {
    if (reason.status === 403) return i18n.t('errors:general.forbidden')
    if (reason.status === 409) return i18n.t('errors:general.conflict')
    return translateApiError(reason.problemDetails, reason.message)
  }
  if (reason instanceof TypeError) return i18n.t('errors:general.networkError')
  return reason instanceof Error ? reason.message : i18n.t('errors:general.unexpectedError')
}

export type SaleStatusKey = 'Completed' | 'Voided' | 'Returned'

/** A sale that has had goods brought back reads as returned, a cancelled one as voided. */
export function saleStatusKey(status: 'Completed' | 'Voided', hasReturns: boolean): SaleStatusKey {
  if (status === 'Voided') return 'Voided'
  return hasReturns ? 'Returned' : 'Completed'
}

export const statusColor = (key: SaleStatusKey): 'success' | 'warning' | 'error' =>
  key === 'Voided' ? 'error' : key === 'Returned' ? 'warning' : 'success'

/** The start of a day, seven days back or so, as an ISO instant the API understands. */
export function rangeStart(range: 'today' | 'week' | 'all'): string | undefined {
  if (range === 'all') return undefined
  const start = new Date()
  start.setHours(0, 0, 0, 0)
  if (range === 'week') start.setDate(start.getDate() - 6)
  return start.toISOString()
}

/** The texts of a data grid (pager, empty state) in the language of the screen. */
export const gridLocaleText = (lang: 'en' | 'tr', noRows?: string) => ({
  ...(lang === 'tr' ? trTR.components.MuiDataGrid.defaultProps.localeText : {}),
  ...(noRows ? { noRowsLabel: noRows } : {}),
})

const moneyFormats = {
  en: new Intl.NumberFormat('en-US', { style: 'currency', currency: 'TRY', currencyDisplay: 'narrowSymbol' }),
  tr: new Intl.NumberFormat('tr-TR', { style: 'currency', currency: 'TRY', currencyDisplay: 'narrowSymbol' }),
}

/** An amount in Turkish lira, written the way the language of the screen writes it (₺1.234,50 or ₺1,234.50). */
export const formatMoney = (value: number, lang: 'en' | 'tr'): string => moneyFormats[lang].format(value)

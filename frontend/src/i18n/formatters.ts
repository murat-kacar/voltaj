import i18n from './index'
import type { ProblemDetails } from '../api'

export function formatCurrency(amount: number, currency: 'USD' | 'TRY' = 'USD', lang?: string): string {
  const currentLang = lang || i18n.language || 'en'
  const locale = currentLang === 'tr' ? 'tr-TR' : 'en-US'
  return new Intl.NumberFormat(locale, {
    style: 'currency',
    currency,
    minimumFractionDigits: 2,
  }).format(amount)
}

export function formatDate(date: string | number | Date, lang?: string, options?: Intl.DateTimeFormatOptions): string {
  const currentLang = lang || i18n.language || 'en'
  const locale = currentLang === 'tr' ? 'tr-TR' : 'en-US'
  const d = typeof date === 'string' || typeof date === 'number' ? new Date(date) : date
  return new Intl.DateTimeFormat(locale, options || {
    year: 'numeric',
    month: 'short',
    day: 'numeric',
    hour: '2-digit',
    minute: '2-digit',
  }).format(d)
}

/**
 * Translates RFC 7807 ProblemDetails into localized human-readable error messages.
 */
export function translateApiError(problem?: ProblemDetails | null, fallbackMessage = ''): string {
  const tDynamic = i18n.t as (key: string) => string
  if (!problem) return fallbackMessage || i18n.t('errors:general.unexpectedError')

  // 1. Check if error code exists in ProblemDetails (e.g. VF-01101, or the business code the API sends as errorCode)
  const code = ((problem as Record<string, unknown>).code as string | undefined) ?? problem.errorCode
  if (code && i18n.exists(`errors:apiCodes.${code}`)) {
    return tDynamic(`errors:apiCodes.${code}`)
  }

  // 2. Check detail or title
  if (problem.detail) {
    if (i18n.exists(`errors:general.${problem.detail}`)) {
      return tDynamic(`errors:general.${problem.detail}`)
    }
    return problem.detail
  }

  if (problem.title) {
    if (i18n.exists(`errors:general.${problem.title}`)) {
      return tDynamic(`errors:general.${problem.title}`)
    }
    return problem.title
  }

  // 3. Fallback to HTTP Status code description
  if (problem.status && i18n.exists(`errors:http.${problem.status}`)) {
    return tDynamic(`errors:http.${problem.status}`)
  }

  return fallbackMessage || i18n.t('errors:general.unexpectedError')
}

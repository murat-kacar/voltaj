import { ApiError } from '../api'
import { i18n, translateApiError } from '../i18n'

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

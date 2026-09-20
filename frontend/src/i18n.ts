import { useTranslation } from 'react-i18next'
export * from './i18n/formatters'
export { default as i18n } from './i18n/index'
export { useTranslation } from 'react-i18next'

export type Language = 'en' | 'tr'

/**
 * Modern reactive i18n hook with backward-compatibility adapter for existing legacy components.
 */
export function useI18n() {
  const { t: translate, i18n: instance } = useTranslation(['common', 'customers', 'quotes', 'workOrders', 'auth', 'errors', 'payments', 'projects', 'reminders'])
  const lang = (instance.language === 'tr' ? 'tr' : 'en') as Language

  const setLang = (newLang: Language) => {
    instance.changeLanguage(newLang)
  }

  // Backward-compatible dictionary proxy for legacy t.xyz access
  const legacyT: Record<string, string> = {
    // Navigation
    overview: translate('common:nav.overview'),
    customers: translate('common:nav.customers'),
    quotes: translate('common:nav.quotes'),
    workOrders: translate('common:nav.workOrders'),
    inventory: translate('common:nav.inventory'),
    payments: translate('common:nav.payments'),
    auditLog: translate('common:nav.auditLog'),
    admin: translate('common:nav.admin'),
    workspace: translate('common:nav.workspace'),
    system: translate('common:nav.system'),
    singleCompany: translate('common:nav.singleCompany'),
    needAHand: translate('common:nav.needAHand'),
    openGuide: translate('common:nav.openGuide'),
    clickToLogout: translate('common:nav.clickToLogout'),
    employee: translate('common:nav.employee'),

    // Top bar & Actions
    searchPlaceholder: translate('common:actions.searchPlaceholder'),
    quickAction: translate('common:actions.quickAction'),
    logout: translate('common:actions.logout'),
    signIn: translate('common:actions.signIn'),
    register: translate('common:actions.register'),
    resetPassword: translate('common:actions.resetPassword'),
    cancel: translate('common:actions.cancel'),
    save: translate('common:actions.save'),
    create: translate('common:actions.create'),
    close: translate('common:actions.close'),

    // Dashboard Overview
    dashboardTitle: translate('common:dashboard.title'),
    dashboardSubtitle: translate('common:dashboard.subtitle'),
    activeOrders: translate('common:dashboard.activeOrders'),
    pendingQuotes: translate('common:dashboard.pendingQuotes'),

    // Statuses
    inProgress: translate('common:status.inProgress'),
    assigned: translate('common:status.assigned'),
    open: translate('common:status.open'),
    completed: translate('common:status.completed'),
    onHold: translate('common:status.onHold'),
    cancelled: translate('common:status.cancelled'),
    unassigned: translate('common:status.unassigned'),
    invoiced: translate('common:status.invoiced'),
    
    // Quote Statuses
    draft: translate('common:quoteStatus.draft'),
    issued: translate('common:quoteStatus.issued'),
    accepted: translate('common:quoteStatus.accepted'),
    rejected: translate('common:quoteStatus.rejected'),
    expired: translate('common:quoteStatus.expired'),

    // Language Toggle
    langEn: translate('common:lang.en'),
    langTr: translate('common:lang.tr'),
  }

  return { lang, setLang, t: legacyT, translate, i18n: instance }
}

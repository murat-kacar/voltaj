import i18n from 'i18next'
import { initReactI18next } from 'react-i18next'

// English translations
import enCommon from '../locales/en/common.json'
import enCustomers from '../locales/en/customers.json'
import enAuth from '../locales/en/auth.json'
import enErrors from '../locales/en/errors.json'
import enPayments from '../locales/en/payments.json'
import enReminders from '../locales/en/reminders.json'
import enServices from '../locales/en/services.json'

// Turkish translations
import trCommon from '../locales/tr/common.json'
import trCustomers from '../locales/tr/customers.json'
import trAuth from '../locales/tr/auth.json'
import trErrors from '../locales/tr/errors.json'
import trPayments from '../locales/tr/payments.json'
import trReminders from '../locales/tr/reminders.json'
import trServices from '../locales/tr/services.json'

export const defaultNS = 'common'
export const resources = {
  en: {
    common: enCommon,
    customers: enCustomers,
    auth: enAuth,
    errors: enErrors,
    payments: enPayments,
    reminders: enReminders,
    services: enServices,
  },
  tr: {
    common: trCommon,
    customers: trCustomers,
    auth: trAuth,
    errors: trErrors,
    payments: trPayments,
    reminders: trReminders,
    services: trServices,
  },
} as const

const storedLang = typeof window !== 'undefined' ? localStorage.getItem('voltflow.lang') : null
const initialLang = storedLang === 'tr' ? 'tr' : 'en'

i18n
  .use(initReactI18next)
  .init({
    resources,
    lng: initialLang,
    fallbackLng: 'en',
    defaultNS,
    interpolation: {
      escapeValue: false, // React already escapes values
    },
    react: {
      useSuspense: false,
    },
  })

if (typeof document !== 'undefined') document.documentElement.lang = initialLang

// Persist language change to localStorage
i18n.on('languageChanged', (lng) => {
  if (typeof window !== 'undefined') {
    localStorage.setItem('voltflow.lang', lng)
    document.documentElement.lang = lng
  }
})

export default i18n

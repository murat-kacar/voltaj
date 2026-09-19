import i18n from 'i18next'
import { initReactI18next } from 'react-i18next'

// English translations
import enCommon from '../locales/en/common.json'
import enCustomers from '../locales/en/customers.json'
import enQuotes from '../locales/en/quotes.json'
import enWorkOrders from '../locales/en/workOrders.json'
import enAuth from '../locales/en/auth.json'
import enErrors from '../locales/en/errors.json'

// Turkish translations
import trCommon from '../locales/tr/common.json'
import trCustomers from '../locales/tr/customers.json'
import trQuotes from '../locales/tr/quotes.json'
import trWorkOrders from '../locales/tr/workOrders.json'
import trAuth from '../locales/tr/auth.json'
import trErrors from '../locales/tr/errors.json'

export const defaultNS = 'common'
export const resources = {
  en: {
    common: enCommon,
    customers: enCustomers,
    quotes: enQuotes,
    workOrders: enWorkOrders,
    auth: enAuth,
    errors: enErrors,
  },
  tr: {
    common: trCommon,
    customers: trCustomers,
    quotes: trQuotes,
    workOrders: trWorkOrders,
    auth: trAuth,
    errors: trErrors,
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

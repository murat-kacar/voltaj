const moneyFormats = {
  en: new Intl.NumberFormat('en-US', { style: 'currency', currency: 'TRY', currencyDisplay: 'narrowSymbol' }),
  tr: new Intl.NumberFormat('tr-TR', { style: 'currency', currency: 'TRY', currencyDisplay: 'narrowSymbol' }),
}

/** An amount in Turkish lira, written the way the language of the screen writes it (₺1.234,50 or ₺1,234.50). */
export const formatMoney = (value: number, lang: 'en' | 'tr'): string => moneyFormats[lang].format(value)

const dayFormats = {
  short: {
    en: new Intl.DateTimeFormat('en-US', { year: 'numeric', month: 'short', day: 'numeric', timeZone: 'UTC' }),
    tr: new Intl.DateTimeFormat('tr-TR', { year: 'numeric', month: 'short', day: 'numeric', timeZone: 'UTC' }),
  },
  long: {
    en: new Intl.DateTimeFormat('en-US', { year: 'numeric', month: 'long', day: 'numeric', timeZone: 'UTC' }),
    tr: new Intl.DateTimeFormat('tr-TR', { year: 'numeric', month: 'long', day: 'numeric', timeZone: 'UTC' }),
  },
}

/** A calendar day ("2026-09-30", no time) written out. It is the day itself, not a moment, so the time zone of the screen cannot move it. */
export const formatDay = (day: string, lang: 'en' | 'tr', month: 'short' | 'long' = 'short'): string =>
  dayFormats[month][lang].format(new Date(`${day.slice(0, 10)}T00:00:00Z`))

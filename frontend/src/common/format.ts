const moneyFormats = {
  en: new Intl.NumberFormat('en-US', { style: 'currency', currency: 'TRY', currencyDisplay: 'narrowSymbol' }),
  tr: new Intl.NumberFormat('tr-TR', { style: 'currency', currency: 'TRY', currencyDisplay: 'narrowSymbol' }),
}

/** An amount in Turkish lira, written the way the language of the screen writes it (₺1.234,50 or ₺1,234.50). */
export const formatMoney = (value: number, lang: 'en' | 'tr'): string => moneyFormats[lang].format(value)

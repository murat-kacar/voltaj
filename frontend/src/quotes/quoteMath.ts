import type { QuoteLineKind, QuoteState } from '../api'
import { divRound, fromCents, toCents } from '../saleMath'

// Mirrors the API's quote rules, so the screen shows the exact totals before a quote is saved. Money is handled in whole
// cents and quantities in hundredths, so a half cent rounds away from zero just like the API's decimal arithmetic. The
// API stays the authority: it works everything out again and refuses what does not add up.

/** The VAT rates a line usually has; a quote can carry another rate if it came from somewhere else. */
export const vatRates = [0, 1, 10, 20] as const
export const defaultVatRate = 20

/** The units a line is usually counted in. Any other can be typed. */
export const commonUnits = ['adet', 'metre', 'saat', 'gün', 'kg', 'set', 'paket'] as const

export type QuoteLineDraft = {
  /** Identifies the line on the screen only. */
  key: string
  kind: QuoteLineKind
  description: string
  /** Up to two decimals. */
  quantity: number
  unit: string
  /** What the customer pays per unit, VAT included. */
  unitPrice: number
  vatRate: number
}

export const newLine = (): QuoteLineDraft => ({
  key: crypto.randomUUID(),
  kind: 'Service',
  description: '',
  quantity: 1,
  unit: commonUnits[0],
  unitPrice: 0,
  vatRate: defaultVatRate,
})

/** A line nobody filled in: it is left out when the quote is saved. */
export const isBlank = (line: QuoteLineDraft): boolean => line.description.trim() === '' && line.unitPrice === 0

const hasAtMostTwoDecimals = (value: number): boolean => Math.abs(value * 100 - Math.round(value * 100)) < 1e-9

/** Whether the API would take the line. */
export const isValid = (line: QuoteLineDraft): boolean =>
  line.description.trim() !== '' &&
  line.quantity > 0 &&
  hasAtMostTwoDecimals(line.quantity) &&
  line.unitPrice >= 0 &&
  line.vatRate >= 0 &&
  line.vatRate <= 100

/** What the customer pays for the line, in cents. */
export const lineCents = (line: Pick<QuoteLineDraft, 'quantity' | 'unitPrice'>): number =>
  divRound(Math.round(line.quantity * 100) * toCents(line.unitPrice), 100)

/** The VAT that is inside a total that already includes it, in cents. */
export function vatCents(totalCents: number, vatRate: number): number {
  const rate = Math.round(vatRate * 100)
  return divRound(totalCents * rate, 10000 + rate)
}

export type VatBucket = { rate: number; gross: number; vat: number }

export type QuoteTotals = { total: number; vat: number; net: number; buckets: VatBucket[] }

/** The totals of lines as they are worked out from quantities and prices. */
export function quoteTotals(lines: Pick<QuoteLineDraft, 'quantity' | 'unitPrice' | 'vatRate'>[]): QuoteTotals {
  return totalsOf(lines.map((line) => ({ rate: line.vatRate, gross: lineCents(line), vat: 0 })).map((entry) => ({ ...entry, vat: vatCents(entry.gross, entry.rate) })))
}

/** The totals of the lines of a saved quote, grouped by VAT rate. */
export function totalsOfLines(lines: { vatRate: number; lineTotal: number; vatAmount: number }[]): QuoteTotals {
  return totalsOf(lines.map((line) => ({ rate: line.vatRate, gross: toCents(line.lineTotal), vat: toCents(line.vatAmount) })))
}

function totalsOf(entries: { rate: number; gross: number; vat: number }[]): QuoteTotals {
  const buckets = new Map<number, { rate: number; gross: number; vat: number }>()
  let total = 0
  let vat = 0
  for (const entry of entries) {
    const bucket = buckets.get(entry.rate) ?? { rate: entry.rate, gross: 0, vat: 0 }
    bucket.gross += entry.gross
    bucket.vat += entry.vat
    buckets.set(entry.rate, bucket)
    total += entry.gross
    vat += entry.vat
  }

  return {
    total: fromCents(total),
    vat: fromCents(vat),
    net: fromCents(total - vat),
    buckets: [...buckets.values()]
      .sort((left, right) => left.rate - right.rate)
      .map((bucket) => ({ rate: bucket.rate, gross: fromCents(bucket.gross), vat: fromCents(bucket.vat) })),
  }
}

/** What a quote is shown as: an issued one whose last day has passed is "lapsed", even before the daily sweep has expired it. */
export type DisplayState = QuoteState | 'Lapsed'

/** Today as the API counts it: a calendar day in UTC. */
export const todayUtc = (): string => new Date().toISOString().slice(0, 10)

export const isLapsed = (state: QuoteState, validUntil: string | null | undefined): boolean =>
  state === 'Issued' && !!validUntil && validUntil < todayUtc()

export const displayState = (state: QuoteState, validUntil: string | null | undefined): DisplayState => (isLapsed(state, validUntil) ? 'Lapsed' : state)

export function stateColor(state: DisplayState): 'default' | 'info' | 'success' | 'error' | 'warning' {
  switch (state) {
    case 'Issued':
      return 'info'
    case 'Accepted':
      return 'success'
    case 'Rejected':
      return 'error'
    case 'Expired':
    case 'Lapsed':
      return 'warning'
    default:
      return 'default'
  }
}

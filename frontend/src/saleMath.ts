// Mirrors the API's sale rules so the screen shows the exact totals before a sale is sent. Money is handled in whole
// cents and quantities in hundredths, so a half cent rounds away from zero just like the API's decimal arithmetic.
// The API stays the authority: it works everything out again and refuses what does not add up.

export type CartLine = {
  key: string
  productId: string | null
  code?: string | null
  name: string
  unit: string
  /** Up to two decimals. */
  quantity: number
  /** What the customer pays per unit, VAT included. */
  unitPrice: number
  vatRate: number
  /** An amount taken off this line. */
  discount: number
  tracksStock: boolean
  stockAvailable?: number | null
}

export type LineTotals = { gross: number; discount: number; share: number; total: number; vat: number }

export type CartTotals = {
  lines: LineTotals[]
  subtotal: number
  lineDiscount: number
  receiptDiscount: number
  grandTotal: number
  vatTotal: number
  /** A discount the rules would refuse: it is larger than the line or the receipt. */
  discountTooLarge: boolean
}

export const toCents = (amount: number): number => Math.round(amount * 100)

export const fromCents = (cents: number): number => cents / 100

/** n / d rounded half away from zero, exact for whole numbers. */
export function divRound(n: number, d: number): number {
  const abs = Math.abs(n)
  let quotient = Math.floor(abs / d)
  let rest = abs - quotient * d
  if (rest < 0) {
    quotient -= 1
    rest += d
  } else if (rest >= d) {
    quotient += 1
    rest -= d
  }
  if (2 * rest >= d) quotient += 1
  return n < 0 ? -quotient : quotient
}

/** Gives the rounding cents to the lines that still have room, so the shares add up to the discount exactly. */
function spreadDiscount(discount: number, nets: number[], netSum: number): number[] {
  const shares = nets.map(() => 0)
  if (discount === 0 || netSum === 0) return shares

  nets.forEach((net, index) => {
    shares[index] = Math.min(net, divRound(discount * net, netSum))
  })

  let left = discount - shares.reduce((sum, share) => sum + share, 0)
  for (let index = 0; index < nets.length && left !== 0; index++) {
    const room = left > 0 ? nets[index] - shares[index] : shares[index]
    const step = Math.min(Math.abs(left), room) * Math.sign(left)
    shares[index] += step
    left -= step
  }
  return shares
}

export function computeCart(lines: CartLine[], receiptDiscountCents: number): CartTotals {
  let discountTooLarge = false

  const gross = lines.map((line) => divRound(Math.round(line.quantity * 100) * toCents(line.unitPrice), 100))
  const lineDiscounts = lines.map((line, index) => {
    const wanted = toCents(line.discount)
    if (wanted < 0 || wanted > gross[index]) discountTooLarge = true
    return Math.min(Math.max(wanted, 0), gross[index])
  })
  const nets = gross.map((amount, index) => amount - lineDiscounts[index])
  const netSum = nets.reduce((sum, net) => sum + net, 0)

  if (receiptDiscountCents < 0 || receiptDiscountCents > netSum) discountTooLarge = true
  const receiptDiscount = Math.min(Math.max(receiptDiscountCents, 0), netSum)
  const shares = spreadDiscount(receiptDiscount, nets, netSum)

  const perLine = lines.map((line, index): LineTotals => {
    const total = nets[index] - shares[index]
    const rate = Math.round(line.vatRate * 100)
    return { gross: gross[index], discount: lineDiscounts[index], share: shares[index], total, vat: divRound(total * rate, 10000 + rate) }
  })

  return {
    lines: perLine,
    subtotal: gross.reduce((sum, amount) => sum + amount, 0),
    lineDiscount: lineDiscounts.reduce((sum, amount) => sum + amount, 0),
    receiptDiscount,
    grandTotal: perLine.reduce((sum, line) => sum + line.total, 0),
    vatTotal: perLine.reduce((sum, line) => sum + line.vat, 0),
    discountTooLarge,
  }
}

export type PaymentPlan = {
  /** The payments can be sent: they cover the total and nothing is over-paid by card or transfer. */
  valid: boolean
  /** What is still missing after the payments entered so far. */
  remaining: number
  change: number
  /** Card and transfer add up to more than the total. */
  overCard: boolean
}

export function planPayments(grandTotal: number, cash: number, card: number, transfer: number): PaymentPlan {
  const nonCash = card + transfer
  const overCard = nonCash > grandTotal
  const cashDue = Math.max(grandTotal - nonCash, 0)
  const remaining = Math.max(cashDue - cash, 0)
  const change = Math.max(cash - cashDue, 0)
  // cash with nothing due in cash is refused by the API, so it is not valid here either
  const valid = grandTotal > 0 && !overCard && remaining === 0 && !(cashDue === 0 && cash > 0)
  return { valid, remaining, change, overCard }
}

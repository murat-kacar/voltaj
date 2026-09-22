import { describe, it, expect } from 'vitest'
import { toCents, fromCents, divRound, computeCart, planPayments, type CartLine } from '../saleMath'

// ── toCents / fromCents ────────────────────────────────────────────────────────
describe('toCents', () => {
  it('converts whole numbers', () => expect(toCents(10)).toBe(1000))
  it('converts decimals', () => expect(toCents(9.99)).toBe(999))
  it('rounds half away from zero', () => expect(toCents(1.005)).toBe(101))
})

describe('fromCents', () => {
  it('converts back to currency units', () => expect(fromCents(1000)).toBe(10))
  it('handles zero', () => expect(fromCents(0)).toBe(0))
})

// ── divRound ───────────────────────────────────────────────────────────────────
describe('divRound', () => {
  it('rounds 1/2 up', () => expect(divRound(1, 2)).toBe(1))
  it('rounds 3/2 up', () => expect(divRound(3, 2)).toBe(2))
  it('rounds negative half away from zero', () => expect(divRound(-1, 2)).toBe(-1))
  it('handles exact division', () => expect(divRound(10, 5)).toBe(2))
})

// ── computeCart ────────────────────────────────────────────────────────────────
const makeLine = (override: Partial<CartLine> = {}): CartLine => ({
  key: 'a',
  productId: null,
  name: 'Test Product',
  unit: 'adet',
  quantity: 1,
  unitPrice: 100,
  vatRate: 20,
  discount: 0,
  tracksStock: false,
  ...override,
})

describe('computeCart', () => {
  it('calculates gross and vat for a single line', () => {
    const cart = computeCart([makeLine()], 0)
    expect(cart.grandTotal).toBe(10000) // 100 TL = 10000 cents
    // VAT = total * vatRate / (10000 + vatRate) = 10000 * 2000 / 12000 ≈ 1667
    expect(cart.vatTotal).toBe(1667)
    expect(cart.discountTooLarge).toBe(false)
  })

  it('applies line discount', () => {
    const cart = computeCart([makeLine({ discount: 10 })], 0) // 10 TL discount
    expect(cart.lineDiscount).toBe(1000) // 1000 cents
    expect(cart.grandTotal).toBe(9000)
  })

  it('applies receipt discount evenly across lines', () => {
    const lines = [makeLine({ key: 'a' }), makeLine({ key: 'b', unitPrice: 200 })]
    const cart = computeCart(lines, 3000) // 30 TL receipt discount
    expect(cart.receiptDiscount).toBe(3000)
    expect(cart.grandTotal).toBe(10000 + 20000 - 3000)
  })

  it('flags discountTooLarge when line discount exceeds gross', () => {
    const cart = computeCart([makeLine({ discount: 200 })], 0) // discount > unitPrice
    expect(cart.discountTooLarge).toBe(true)
  })

  it('flags discountTooLarge when receipt discount exceeds subtotal', () => {
    const cart = computeCart([makeLine()], 99999)
    expect(cart.discountTooLarge).toBe(true)
  })

  it('returns zero totals for empty cart', () => {
    const cart = computeCart([], 0)
    expect(cart.grandTotal).toBe(0)
    expect(cart.vatTotal).toBe(0)
  })
})

// ── planPayments ───────────────────────────────────────────────────────────────
describe('planPayments', () => {
  it('validates when cash covers the remainder', () => {
    const plan = planPayments(10000, 10000, 0, 0) // 100 TL cash
    expect(plan.valid).toBe(true)
    expect(plan.remaining).toBe(0)
    expect(plan.change).toBe(0)
  })

  it('calculates change when overpaid in cash', () => {
    const plan = planPayments(9000, 10000, 0, 0)
    expect(plan.valid).toBe(true)
    expect(plan.change).toBe(1000)
  })

  it('returns remaining when underpaid', () => {
    const plan = planPayments(10000, 5000, 0, 0)
    expect(plan.valid).toBe(false)
    expect(plan.remaining).toBe(5000)
  })

  it('flags overCard when card+transfer exceed total', () => {
    const plan = planPayments(10000, 0, 8000, 5000)
    expect(plan.overCard).toBe(true)
    expect(plan.valid).toBe(false)
  })

  it('validates card-only payment', () => {
    const plan = planPayments(10000, 0, 10000, 0)
    expect(plan.valid).toBe(true)
    expect(plan.change).toBe(0)
  })

  it('rejects cash when nothing is due in cash', () => {
    // full card payment + extra cash is refused
    const plan = planPayments(10000, 500, 10000, 0)
    expect(plan.valid).toBe(false)
  })
})

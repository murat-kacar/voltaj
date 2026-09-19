export type SaleStatusKey = 'Completed' | 'Voided' | 'Returned'

/** A sale that has had goods brought back reads as returned, a cancelled one as voided. */
export function saleStatusKey(status: 'Completed' | 'Voided', hasReturns: boolean): SaleStatusKey {
  if (status === 'Voided') return 'Voided'
  return hasReturns ? 'Returned' : 'Completed'
}

export const statusColor = (key: SaleStatusKey): 'success' | 'warning' | 'error' =>
  key === 'Voided' ? 'error' : key === 'Returned' ? 'warning' : 'success'

/** The start of a day, seven days back or so, as an ISO instant the API understands. */
export function rangeStart(range: 'today' | 'week' | 'all'): string | undefined {
  if (range === 'all') return undefined
  const start = new Date()
  start.setHours(0, 0, 0, 0)
  if (range === 'week') start.setDate(start.getDate() - 6)
  return start.toISOString()
}

import { apiRequest } from './_base'

export interface DeadLetterMessage {
  id: string
  aggregateId: string
  aggregateType: string
  eventType: string
  payload: string
  error: string
  occurredOn: string
}

export const operationsApi = {
  listDeadLetters: () => apiRequest<DeadLetterMessage[]>('/api/operations/outbox/dead-letter'),
  replayDeadLetter: (id: string) => apiRequest<void>(`/api/operations/outbox/${id}/replay`, { method: 'POST' }),
}

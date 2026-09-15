import { useEffect, useState } from 'react'
import { ApiError, customersApi, workOrdersApi, type Customer, type ProblemDetails } from './api'

type Props = {
  isOpen: boolean
  onClose: () => void
  onSuccess: () => void
}

export function CreateWorkOrderModal({ isOpen, onClose, onSuccess }: Props) {
  const [customers, setCustomers] = useState<Customer[]>([])
  const [customersError, setCustomersError] = useState('')
  const [customerId, setCustomerId] = useState('')
  const [title, setTitle] = useState('')
  const [loading, setLoading] = useState(false)
  const [fieldErrors, setFieldErrors] = useState<Record<string, string>>({})
  const [problem, setProblem] = useState<ProblemDetails | null>(null)

  useEffect(() => {
    if (!isOpen) return
    let ignore = false
    customersApi
      .list()
      .then((data) => {
        if (!ignore) {
          setCustomers(data)
          setCustomersError('')
        }
      })
      .catch((err: unknown) => {
        if (!ignore) {
          setCustomersError(err instanceof Error ? err.message : 'Failed to load customers.')
        }
      })
    return () => {
      ignore = true
    }
  }, [isOpen])

  if (!isOpen) return null

  function validate() {
    const errors: Record<string, string> = {}
    if (!customerId.trim()) errors.customerId = 'Please select a customer.'
    if (!title.trim()) errors.title = 'Work order title is required.'
    setFieldErrors(errors)
    return Object.keys(errors).length === 0
  }

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault()
    setProblem(null)
    if (!validate()) return

    setLoading(true)
    try {
      await workOrdersApi.create({
        customerId: customerId.trim(),
        title: title.trim(),
      })
      setTitle('')
      setCustomerId('')
      onSuccess()
      onClose()
    } catch (err: unknown) {
      if (err instanceof ApiError && err.problemDetails) {
        setProblem(err.problemDetails)
      } else if (err instanceof Error) {
        setProblem({
          title: 'Failed to create work order',
          detail: err.message,
        })
      } else {
        setProblem({
          title: 'Unexpected error',
          detail: 'An unknown error occurred while creating the work order.',
        })
      }
    } finally {
      setLoading(false)
    }
  }

  return (
    <div className="modal-backdrop" onClick={onClose}>
      <div className="form-modal" onClick={(e) => e.stopPropagation()}>
        <button className="modal-close" onClick={onClose} disabled={loading} title="Close">
          ×
        </button>
        <p className="eyebrow">Field operations</p>
        <h2>New work order</h2>

        {problem && (
          <div className="problem-details">
            <strong>{problem.title ?? 'Error occurred'}</strong>
            <span>{problem.detail}</span>
            {problem.traceId && <small>Trace: {problem.traceId}</small>}
          </div>
        )}

        <form onSubmit={handleSubmit} data-screen-id="SCR-0310">
          <div className="form-group">
            <label htmlFor="wo-customer">Customer *</label>
            <select
              id="wo-customer"
              data-testid="03101-customer-select"
              value={customerId}
              onChange={(e) => setCustomerId(e.target.value)}
              disabled={loading}
              autoFocus
            >
              <option value="">-- Select customer --</option>
              {customers.map((c) => (
                <option key={c.id} value={c.id}>
                  {c.fullName} ({c.email})
                </option>
              ))}
            </select>
            {customersError && <span className="field-error">{customersError}</span>}
            {fieldErrors.customerId && <span className="field-error">{fieldErrors.customerId}</span>}
          </div>

          <div className="form-group">
            <label htmlFor="wo-title">Job description / title *</label>
            <input
              id="wo-title"
              data-testid="03101-title-input"
              type="text"
              placeholder="e.g. Main Distribution Panel Repair & Certification"
              value={title}
              onChange={(e) => setTitle(e.target.value)}
              disabled={loading}
            />
            {fieldErrors.title && <span className="field-error">{fieldErrors.title}</span>}
          </div>

          <div className="modal-actions">
            <button
              type="button"
              data-testid="03101-cancel-btn"
              className="secondary-button"
              onClick={onClose}
              disabled={loading}
            >
              Cancel
            </button>
            <button
              type="submit"
              data-testid="03101-submit-btn"
              className="primary-button"
              disabled={loading}
            >
              {loading ? 'Creating…' : 'Create work order'}
            </button>
          </div>
        </form>
      </div>
    </div>
  )
}

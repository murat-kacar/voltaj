import { useState } from 'react'
import { ApiError, customersApi, type ProblemDetails } from './api'

type Props = {
  isOpen: boolean
  onClose: () => void
  onSuccess: () => void
}

export function CreateCustomerModal({ isOpen, onClose, onSuccess }: Props) {
  const [fullName, setFullName] = useState('')
  const [email, setEmail] = useState('')
  const [phone, setPhone] = useState('')
  const [loading, setLoading] = useState(false)
  const [fieldErrors, setFieldErrors] = useState<Record<string, string>>({})
  const [problem, setProblem] = useState<ProblemDetails | null>(null)

  if (!isOpen) return null

  function validate() {
    const errors: Record<string, string> = {}
    if (!fullName.trim()) errors.fullName = 'Full name is required.'
    if (!email.trim()) {
      errors.email = 'Email address is required.'
    } else if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email.trim())) {
      errors.email = 'Please enter a valid email address.'
    }
    if (!phone.trim()) errors.phone = 'Phone number is required.'
    setFieldErrors(errors)
    return Object.keys(errors).length === 0
  }

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault()
    setProblem(null)
    if (!validate()) return

    setLoading(true)
    try {
      await customersApi.create({
        fullName: fullName.trim(),
        email: email.trim(),
        phone: phone.trim(),
      })
      setFullName('')
      setEmail('')
      setPhone('')
      onSuccess()
      onClose()
    } catch (err: unknown) {
      if (err instanceof ApiError && err.problemDetails) {
        setProblem(err.problemDetails)
      } else if (err instanceof Error) {
        setProblem({
          title: 'Failed to create customer',
          detail: err.message,
        })
      } else {
        setProblem({
          title: 'Unexpected error',
          detail: 'An unknown error occurred while creating the customer.',
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
        <p className="eyebrow">Customer management</p>
        <h2>Add new customer</h2>

        {problem && (
          <div className="problem-details">
            <strong>{problem.title ?? 'Error occurred'}</strong>
            <span>{problem.detail}</span>
            {problem.traceId && <small>Trace: {problem.traceId}</small>}
          </div>
        )}

        <form onSubmit={handleSubmit} data-screen-id="SCR-0210">
          <div className="form-group">
            <label htmlFor="customer-name">Full name *</label>
            <input
              id="customer-name"
              data-testid="02101-fullname-input"
              type="text"
              placeholder="e.g. Artemis Logistics A.Ş."
              value={fullName}
              onChange={(e) => setFullName(e.target.value)}
              disabled={loading}
              autoFocus
            />
            {fieldErrors.fullName && <span className="field-error">{fieldErrors.fullName}</span>}
          </div>

          <div className="form-group">
            <label htmlFor="customer-email">Email address *</label>
            <input
              id="customer-email"
              data-testid="02101-email-input"
              type="email"
              placeholder="contact@company.com"
              value={email}
              onChange={(e) => setEmail(e.target.value)}
              disabled={loading}
            />
            {fieldErrors.email && <span className="field-error">{fieldErrors.email}</span>}
          </div>

          <div className="form-group">
            <label htmlFor="customer-phone">Phone number *</label>
            <input
              id="customer-phone"
              data-testid="02101-phone-input"
              type="tel"
              placeholder="+90 532 000 00 00"
              value={phone}
              onChange={(e) => setPhone(e.target.value)}
              disabled={loading}
            />
            {fieldErrors.phone && <span className="field-error">{fieldErrors.phone}</span>}
          </div>

          <div className="modal-actions">
            <button
              type="button"
              data-testid="02101-cancel-btn"
              className="secondary-button"
              onClick={onClose}
              disabled={loading}
            >
              Cancel
            </button>
            <button
              type="submit"
              data-testid="02101-submit-btn"
              className="primary-button"
              disabled={loading}
            >
              {loading ? 'Creating…' : 'Create customer'}
            </button>
          </div>
        </form>
      </div>
    </div>
  )
}

import { useState } from 'react'
import { ApiError, customersApi, type ProblemDetails } from './api'
import { useTranslation, translateApiError } from './i18n'

type Props = {
  isOpen: boolean
  onClose: () => void
  onSuccess: () => void
}

export function CreateCustomerModal({ isOpen, onClose, onSuccess }: Props) {
  const { t } = useTranslation(['customers', 'common', 'errors'])
  const [fullName, setFullName] = useState('')
  const [email, setEmail] = useState('')
  const [phone, setPhone] = useState('')
  const [loading, setLoading] = useState(false)
  const [fieldErrors, setFieldErrors] = useState<Record<string, string>>({})
  const [problem, setProblem] = useState<ProblemDetails | null>(null)

  if (!isOpen) return null

  function validate() {
    const errors: Record<string, string> = {}
    if (!fullName.trim()) errors.fullName = t('customers:validation.fullNameRequired')
    if (!email.trim()) {
      errors.email = t('customers:validation.emailRequired')
    } else if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email.trim())) {
      errors.email = t('customers:validation.emailInvalid')
    }
    if (!phone.trim()) errors.phone = t('customers:validation.phoneRequired')
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
          title: t('customers:errors.createFailed'),
          detail: err.message,
        })
      } else {
        setProblem({
          title: t('errors:general.unexpectedError'),
          detail: t('errors:general.unexpectedError'),
        })
      }
    } finally {
      setLoading(false)
    }
  }

  return (
    <div className="modal-backdrop" onClick={onClose}>
      <div className="form-modal" onClick={(e) => e.stopPropagation()}>
        <button className="modal-close" onClick={onClose} disabled={loading} title={t('common:actions.close')}>
          ×
        </button>
        <p className="eyebrow">{t('customers:eyebrow')}</p>
        <h2>{t('customers:form.title')}</h2>

        {problem && (
          <div className="problem-details">
            <strong>{translateApiError(problem)}</strong>
            {problem.detail && <span>{problem.detail}</span>}
            {problem.traceId && <small>Trace: {problem.traceId}</small>}
          </div>
        )}

        <form onSubmit={handleSubmit} data-screen-id="SCR-0210">
          <div className="form-group">
            <label htmlFor="customer-name">{t('customers:form.fullName')} *</label>
            <input
              id="customer-name"
              data-testid="02101-fullname-input"
              type="text"
              placeholder={t('customers:form.fullNamePlaceholder')}
              value={fullName}
              onChange={(e) => setFullName(e.target.value)}
              disabled={loading}
              autoFocus
            />
            {fieldErrors.fullName && <span className="field-error">{fieldErrors.fullName}</span>}
          </div>

          <div className="form-group">
            <label htmlFor="customer-email">{t('customers:form.email')} *</label>
            <input
              id="customer-email"
              data-testid="02101-email-input"
              type="email"
              placeholder={t('customers:form.emailPlaceholder')}
              value={email}
              onChange={(e) => setEmail(e.target.value)}
              disabled={loading}
            />
            {fieldErrors.email && <span className="field-error">{fieldErrors.email}</span>}
          </div>

          <div className="form-group">
            <label htmlFor="customer-phone">{t('customers:form.phone')} *</label>
            <input
              id="customer-phone"
              data-testid="02101-phone-input"
              type="tel"
              placeholder={t('customers:form.phonePlaceholder')}
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
              {t('customers:form.cancel')}
            </button>
            <button
              type="submit"
              data-testid="02101-submit-btn"
              className="primary-button"
              disabled={loading}
            >
              {loading ? t('customers:form.creating') : t('customers:form.submit')}
            </button>
          </div>
        </form>
      </div>
    </div>
  )
}

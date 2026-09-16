import { useEffect, useState } from 'react'
import { ApiError, customersApi, workOrdersApi, type Customer, type ProblemDetails } from './api'
import { useTranslation, translateApiError } from './i18n'

type Props = {
  isOpen: boolean
  onClose: () => void
  onSuccess: () => void
}

export function CreateWorkOrderModal({ isOpen, onClose, onSuccess }: Props) {
  const { t } = useTranslation(['workOrders', 'common', 'errors'])
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
          setCustomersError(err instanceof Error ? err.message : t('workOrders:errors.loadCustomersFailed'))
        }
      })
    return () => {
      ignore = true
    }
  }, [isOpen, t])

  if (!isOpen) return null

  function validate() {
    const errors: Record<string, string> = {}
    if (!customerId.trim()) errors.customerId = t('workOrders:validation.customerRequired')
    if (!title.trim()) errors.title = t('workOrders:validation.titleRequired')
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
          title: t('workOrders:errors.createFailed'),
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
        <p className="eyebrow">{t('workOrders:eyebrow')}</p>
        <h2>{t('workOrders:form.title')}</h2>

        {problem && (
          <div className="problem-details">
            <strong>{translateApiError(problem)}</strong>
            {problem.detail && <span>{problem.detail}</span>}
            {problem.traceId && <small>Trace: {problem.traceId}</small>}
          </div>
        )}

        <form onSubmit={handleSubmit} data-screen-id="SCR-0310">
          <div className="form-group">
            <label htmlFor="wo-customer">{t('workOrders:form.customer')} *</label>
            <select
              id="wo-customer"
              data-testid="03101-customer-select"
              value={customerId}
              onChange={(e) => setCustomerId(e.target.value)}
              disabled={loading}
              autoFocus
            >
              <option value="">-- {t('workOrders:form.selectCustomer')} --</option>
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
            <label htmlFor="wo-title">{t('workOrders:form.orderTitle')} *</label>
            <input
              id="wo-title"
              data-testid="03101-title-input"
              type="text"
              placeholder={t('workOrders:form.orderTitlePlaceholder')}
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
              {t('workOrders:form.cancel')}
            </button>
            <button
              type="submit"
              data-testid="03101-submit-btn"
              className="primary-button"
              disabled={loading}
            >
              {loading ? t('workOrders:form.creating') : t('workOrders:form.submit')}
            </button>
          </div>
        </form>
      </div>
    </div>
  )
}

import { useState } from 'react'
import { quotesApi, type Quote, ApiError, type ProblemDetails } from './api'
import { useTranslation, formatCurrency, translateApiError } from './i18n'

export function QuoteDetailDrawer({
  quote,
  isOpen,
  onClose,
  onUpdated,
}: {
  quote: Quote | null
  isOpen: boolean
  onClose: () => void
  onUpdated: () => void
}) {
  const { t, i18n } = useTranslation(['quotes', 'common', 'errors'])
  const [loading, setLoading] = useState(false)
  const [problem, setProblem] = useState<ProblemDetails | null>(null)
  const [depositAmount, setDepositAmount] = useState(0)
  const [reqDepositPercentage, setReqDepositPercentage] = useState(30)

  if (!quote) return null

  const currency = i18n.language === 'tr' ? 'TRY' : 'USD'

  const handleApiError = (err: unknown, defaultTitle: string) => {
    if (err instanceof ApiError && err.problemDetails) {
      setProblem(err.problemDetails)
    } else {
      setProblem({ title: defaultTitle, detail: err instanceof Error ? err.message : String(err) })
    }
  }

  const handleIssue = async () => {
    setLoading(true)
    setProblem(null)
    try {
      await quotesApi.issue(quote.id)
      onUpdated()
    } catch (err) {
      handleApiError(err, t('quotes:errors.issueFailed'))
    } finally {
      setLoading(false)
    }
  }

  const handleAccept = async () => {
    setLoading(true)
    setProblem(null)
    try {
      await quotesApi.accept(quote.id, reqDepositPercentage)
      onUpdated()
    } catch (err) {
      handleApiError(err, t('quotes:errors.acceptFailed'))
    } finally {
      setLoading(false)
    }
  }

  const handlePayDeposit = async () => {
    if (depositAmount <= 0) return
    setLoading(true)
    setProblem(null)
    try {
      await quotesApi.payDeposit(quote.id, depositAmount)
      onUpdated()
    } catch (err) {
      handleApiError(err, t('quotes:errors.payDepositFailed'))
    } finally {
      setLoading(false)
    }
  }

  const getStatusLabel = (state: string) => {
    const key = state.charAt(0).toLowerCase() + state.slice(1)
    const translationKey = `common:quoteStatus.${key}`
    return i18n.exists(translationKey) ? t(translationKey as any) : state
  }

  return (
    <>
      <div className={`drawer-backdrop ${isOpen ? 'open' : ''}`} onClick={onClose} />
      <div className={`drawer ${isOpen ? 'open' : ''}`}>
        <div className="drawer-header">
          <div>
            <h2>{quote.number}</h2>
            <p>{quote.title}</p>
          </div>
          <button className="icon-button" onClick={onClose} title={t('common:actions.close')}>
            ✕
          </button>
        </div>
        <div className="drawer-body">
          {problem && (
            <div className="state-panel error-state" style={{ marginBottom: 16 }} data-testid="02401-problem-details">
              <b>{translateApiError(problem)}</b>
              {problem.detail && <p>{problem.detail}</p>}
            </div>
          )}

          <div className="detail-card">
            <div className="detail-grid">
              <div className="detail-field">
                <small>{t('quotes:drawer.status')}</small>
                <span>{getStatusLabel(quote.state)}</span>
              </div>
              <div className="detail-field">
                <small>{t('quotes:drawer.totalAmount')}</small>
                <span>{formatCurrency(quote.total, currency)}</span>
              </div>
            </div>
          </div>

          {quote.state === 'Draft' && (
            <div className="detail-card">
              <button className="primary-button" data-testid="02301-issue-btn" onClick={handleIssue} disabled={loading} style={{ width: '100%' }}>
                {loading ? t('quotes:drawer.processing') : t('quotes:drawer.issue')}
              </button>
            </div>
          )}

          {quote.state === 'Issued' && (
            <div className="detail-card">
              <div className="detail-field">
                <small>{t('quotes:drawer.depositPercentage')}</small>
                <input
                  type="number"
                  min="0"
                  max="100"
                  data-testid="02401-deposit-pct-input"
                  value={reqDepositPercentage}
                  onChange={(e) => setReqDepositPercentage(Number(e.target.value))}
                  disabled={loading}
                />
              </div>
              <button className="primary-button" data-testid="02401-accept-btn" onClick={handleAccept} disabled={loading} style={{ width: '100%', marginTop: 12 }}>
                {loading ? t('quotes:drawer.processing') : t('quotes:drawer.accept')}
              </button>
            </div>
          )}

          {quote.state === 'Accepted' && quote.requiredDepositPercentage > 0 && (
            <div className="detail-card">
              <div className="detail-field" style={{ marginBottom: 16 }}>
                <small>{t('quotes:table.depositRequired')}</small>
                <div>{t('quotes:table.depositRequired')}: {quote.requiredDepositPercentage}% ({formatCurrency((quote.total * quote.requiredDepositPercentage) / 100, currency)})</div>
                <div>{t('quotes:table.depositPaid')}: {formatCurrency(quote.depositPaidAmount, currency)}</div>
              </div>
              
              {quote.depositPaidAmount < ((quote.total * quote.requiredDepositPercentage) / 100) && (
                <div style={{ display: 'flex', gap: 8, marginTop: 12 }}>
                  <input
                    type="number"
                    min="0"
                    placeholder={t('quotes:drawer.depositAmount')}
                    data-testid="02401-pay-deposit-input"
                    value={depositAmount || ''}
                    onChange={(e) => setDepositAmount(Number(e.target.value))}
                    disabled={loading}
                  />
                  <button className="secondary-button" data-testid="02401-pay-deposit-btn" onClick={handlePayDeposit} disabled={loading || depositAmount <= 0}>
                    {t('quotes:drawer.payDeposit')}
                  </button>
                </div>
              )}
            </div>
          )}

        </div>
        <div className="drawer-footer">
          <button className="secondary-button" onClick={onClose}>
            {t('common:actions.close')}
          </button>
        </div>
      </div>
    </>
  )
}

import { useState } from 'react'
import { quotesApi, type Quote, ApiError, type ProblemDetails } from './api'

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
  const [loading, setLoading] = useState(false)
  const [problem, setProblem] = useState<ProblemDetails | null>(null)
  const [depositAmount, setDepositAmount] = useState(0)
  const [reqDepositPercentage, setReqDepositPercentage] = useState(30)

  if (!quote) return null

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
      handleApiError(err, 'Failed to issue quote')
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
      handleApiError(err, 'Failed to accept quote')
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
      handleApiError(err, 'Failed to pay deposit')
    } finally {
      setLoading(false)
    }
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
          <button className="icon-button" onClick={onClose}>
            ✕
          </button>
        </div>
        <div className="drawer-body">
          {problem && (
            <div className="state-panel error-state" style={{ marginBottom: 16 }} data-testid="02401-problem-details">
              <b>{problem.title}</b>
              <p>{problem.detail}</p>
            </div>
          )}

          <div className="detail-card">
            <div className="detail-grid">
              <div className="detail-field">
                <small>Status</small>
                <span>{quote.state}</span>
              </div>
              <div className="detail-field">
                <small>Total Value</small>
                <span>₺{quote.total.toLocaleString('tr-TR')}</span>
              </div>
            </div>
          </div>

          {quote.state === 'Draft' && (
            <div className="detail-card">
              <button className="primary-button" data-testid="02301-issue-btn" onClick={handleIssue} disabled={loading} style={{ width: '100%' }}>
                {loading ? 'Issuing...' : 'Issue Quote'}
              </button>
            </div>
          )}

          {quote.state === 'Issued' && (
            <div className="detail-card">
              <div className="detail-field">
                <small>Required Deposit (%)</small>
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
                {loading ? 'Accepting...' : 'Accept Quote'}
              </button>
            </div>
          )}

          {quote.state === 'Accepted' && quote.requiredDepositPercentage > 0 && (
            <div className="detail-card">
              <div className="detail-field" style={{ marginBottom: 16 }}>
                <small>Deposit Information</small>
                <div>Required: {quote.requiredDepositPercentage}% (₺{((quote.total * quote.requiredDepositPercentage) / 100).toLocaleString('tr-TR')})</div>
                <div>Paid: ₺{quote.depositPaidAmount.toLocaleString('tr-TR')}</div>
              </div>
              
              {quote.depositPaidAmount < ((quote.total * quote.requiredDepositPercentage) / 100) && (
                <div style={{ display: 'flex', gap: 8, marginTop: 12 }}>
                  <input
                    type="number"
                    min="0"
                    placeholder="Amount to pay"
                    data-testid="02401-pay-deposit-input"
                    value={depositAmount || ''}
                    onChange={(e) => setDepositAmount(Number(e.target.value))}
                    disabled={loading}
                  />
                  <button className="secondary-button" data-testid="02401-pay-deposit-btn" onClick={handlePayDeposit} disabled={loading || depositAmount <= 0}>
                    Pay Deposit
                  </button>
                </div>
              )}
            </div>
          )}

        </div>
        <div className="drawer-footer">
          <button className="secondary-button" onClick={onClose}>
            Close
          </button>
        </div>
      </div>
    </>
  )
}

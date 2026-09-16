import { useEffect, useMemo, useState } from 'react'
import { customersApi, quotesApi } from './api'
import { CreateCustomerModal } from './CreateCustomerModal'
import { CreateQuoteModal } from './CreateQuoteModal'
import { QuoteDetailDrawer } from './QuoteDetailDrawer'
import { useTranslation, formatCurrency } from './i18n'
import type { Quote } from './api'

type CustomerRow = { name: string; type: string; contact: string; phone: string; status: string; value: string }
export function CustomersView() {
  const { t } = useTranslation(['customers', 'common'])
  const [query, setQuery] = useState('')
  const [items, setItems] = useState<CustomerRow[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')
  const [showModal, setShowModal] = useState(false)
  const [reloadKey, setReloadKey] = useState(0)

  useEffect(() => {
    let ignore = false
    customersApi
      .list()
      .then((result) => {
        if (!ignore) {
          setItems(
            result.map((customer) => ({
              name: customer.fullName,
              type: 'Customer',
              contact: customer.email,
              phone: customer.phone,
              status: customer.isActive ? t('common:status.active') : t('common:status.inactive'),
              value: '—',
            }))
          )
          setError('')
          setLoading(false)
        }
      })
      .catch((reason: unknown) => {
        if (!ignore) {
          setError(reason instanceof Error ? reason.message : t('customers:errors.loadFailed'))
          setLoading(false)
        }
      })
    return () => {
      ignore = true
    }
  }, [reloadKey, t])

  const filtered = useMemo(
    () => items.filter((customer) => `${customer.name} ${customer.contact}`.toLowerCase().includes(query.toLowerCase())),
    [items, query]
  )

  return (
    <div className="module-view">
      <div className="module-heading">
        <div>
          <p className="eyebrow">{t('customers:eyebrow')}</p>
          <h1>{t('customers:title')}</h1>
          <p className="heading-copy">{t('customers:subtitle')}</p>
        </div>
        <button className="primary-button" data-testid="02101-add-customer-btn" onClick={() => setShowModal(true)}>
          <span>+</span> {t('customers:newCustomer')}
        </button>
      </div>

      {loading && <div className="state-panel">{t('common:tables.loading')}</div>}
      {error && <div className="state-panel error-state">{error}</div>}

      {!loading && !error && (
        <>
          <div className="module-toolbar">
            <label className="module-search">
              ⌕
              <input
                value={query}
                onChange={(event) => setQuery(event.target.value)}
                placeholder={t('customers:searchPlaceholder')}
              />
            </label>
            <button className="filter-button">{t('common:actions.filter')}⌄</button>
          </div>

          <div className="panel module-table">
            {filtered.length === 0 ? (
              <div className="state-panel">{t('common:tables.noData')}</div>
            ) : (
              <>
                <div className="module-table-head">
                  <span>{t('customers:table.name')}</span>
                  <span>{t('customers:table.contact')}</span>
                  <span>{t('customers:table.type')}</span>
                  <span>{t('customers:table.value')}</span>
                  <span>{t('customers:table.status')}</span>
                </div>
                {filtered.map((customer) => (
                  <div className="module-table-row" key={customer.name}>
                    <span>
                      <b>{customer.name}</b>
                      <small>{customer.phone}</small>
                    </span>
                    <span>{customer.contact}</span>
                    <span>
                      <i className={`type-pill ${customer.type === 'Candidate' ? 'candidate' : ''}`}>
                        {customer.type}
                      </i>
                    </span>
                    <span>{customer.value}</span>
                    <span>
                      <i className="status-dot green" />
                      {customer.status}
                    </span>
                  </div>
                ))}
              </>
            )}
          </div>
        </>
      )}

      <CreateCustomerModal
        isOpen={showModal}
        onClose={() => setShowModal(false)}
        onSuccess={() => setReloadKey((k) => k + 1)}
      />
    </div>
  )
}

export function QuotesView() {
  const { t, i18n } = useTranslation(['quotes', 'common'])
  const [query, setQuery] = useState('')
  const [items, setItems] = useState<Quote[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')
  const [showModal, setShowModal] = useState(false)
  const [selectedQuote, setSelectedQuote] = useState<Quote | null>(null)
  const [reloadKey, setReloadKey] = useState(0)

  useEffect(() => {
    let ignore = false
    quotesApi
      .list()
      .then((result) => {
        if (!ignore) {
          setItems(result)
          setError('')
          setLoading(false)
        }
      })
      .catch((reason: unknown) => {
        if (!ignore) {
          setError(reason instanceof Error ? reason.message : t('quotes:errors.loadFailed'))
          setLoading(false)
        }
      })
    return () => {
      ignore = true
    }
  }, [reloadKey, t])

  const filtered = useMemo(
    () => items.filter((quote) => `${quote.number} ${quote.customerId} ${quote.title}`.toLowerCase().includes(query.toLowerCase())),
    [items, query]
  )

  const getStatusLabel = (state: string) => {
    const key = state.charAt(0).toLowerCase() + state.slice(1)
    const translationKey = `common:quoteStatus.${key}`
    return i18n.exists(translationKey) ? t(translationKey as any) : state
  }

  return (
    <div className="module-view">
      <div className="module-heading">
        <div>
          <p className="eyebrow">{t('quotes:eyebrow')}</p>
          <h1>{t('quotes:title')}</h1>
          <p className="heading-copy">{t('quotes:subtitle')}</p>
        </div>
        <button className="primary-button" data-testid="02301-new-quote-btn" onClick={() => setShowModal(true)}>
          <span>+</span> {t('quotes:newQuote')}
        </button>
      </div>

      {loading && <div className="state-panel">{t('common:tables.loading')}</div>}
      {error && <div className="state-panel error-state">{error}</div>}

      {!loading && !error && (
        <>
          <div className="module-toolbar">
            <label className="module-search">
              ⌕
              <input
                value={query}
                onChange={(event) => setQuery(event.target.value)}
                placeholder={t('quotes:searchPlaceholder')}
              />
            </label>
            <button className="filter-button">{t('common:actions.filter')}⌄</button>
          </div>

          <div className="panel module-table">
            {filtered.length === 0 ? (
              <div className="state-panel">{t('common:tables.noData')}</div>
            ) : (
              <>
                <div className="module-table-head quote-head">
                  <span>{t('quotes:table.quote')}</span>
                  <span>{t('quotes:table.customer')}</span>
                  <span>{t('quotes:table.total')}</span>
                  <span>{t('quotes:table.status')}</span>
                </div>
                {filtered.map((quote) => (
                  <div 
                    className="module-table-row quote-row clickable-row" 
                    key={quote.number} 
                    onClick={() => setSelectedQuote(quote)}
                  >
                    <span>
                      <b>{quote.number}</b>
                      <small>{quote.title}</small>
                    </span>
                    <span>{quote.customerId}</span>
                    <span>{formatCurrency(quote.total, i18n.language === 'tr' ? 'TRY' : 'USD')}</span>
                    <span>
                      <i
                        className={`status-dot ${
                          quote.state === 'Accepted'
                            ? 'green'
                            : quote.state === 'Rejected'
                            ? 'red'
                            : quote.state === 'Issued'
                            ? 'blue'
                            : 'amber'
                        }`}
                      />
                      {getStatusLabel(quote.state)}
                    </span>
                  </div>
                ))}
              </>
            )}
          </div>
        </>
      )}

      <CreateQuoteModal
        isOpen={showModal}
        onClose={() => setShowModal(false)}
        onSuccess={() => setReloadKey((k) => k + 1)}
      />

      <QuoteDetailDrawer
        quote={selectedQuote}
        isOpen={Boolean(selectedQuote)}
        onClose={() => setSelectedQuote(null)}
        onUpdated={() => {
          setSelectedQuote(null)
          setReloadKey((k) => k + 1)
        }}
      />
    </div>
  )
}

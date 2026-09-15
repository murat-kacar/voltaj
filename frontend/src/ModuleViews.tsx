import { useEffect, useMemo, useState } from 'react'
import { customersApi, quotesApi } from './api'
import { CreateCustomerModal } from './CreateCustomerModal'
import { CreateQuoteModal } from './CreateQuoteModal'
import { QuoteDetailDrawer } from './QuoteDetailDrawer'
import { useI18n } from './i18n'
import type { Quote } from './api'

type CustomerRow = { name: string; type: string; contact: string; phone: string; status: string; value: string }
export function CustomersView() {
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
              status: customer.isActive ? 'Active' : 'Inactive',
              value: '—',
            }))
          )
          setError('')
          setLoading(false)
        }
      })
      .catch((reason: unknown) => {
        if (!ignore) {
          setError(reason instanceof Error ? reason.message : 'Customers could not be loaded.')
          setLoading(false)
        }
      })
    return () => {
      ignore = true
    }
  }, [reloadKey])

  const filtered = useMemo(
    () => items.filter((customer) => `${customer.name} ${customer.contact}`.toLowerCase().includes(query.toLowerCase())),
    [items, query]
  )

  return (
    <div className="module-view">
      <div className="module-heading">
        <div>
          <p className="eyebrow">Relationship workspace</p>
          <h1>Customers</h1>
          <p className="heading-copy">Keep every customer relationship and candidate conversion in view.</p>
        </div>
        <button className="primary-button" data-testid="02101-add-customer-btn" onClick={() => setShowModal(true)}>
          <span>+</span> Add customer
        </button>
      </div>

      {loading && <div className="state-panel">Loading customers…</div>}
      {error && <div className="state-panel error-state">{error}</div>}

      {!loading && !error && (
        <>
          <div className="module-toolbar">
            <label className="module-search">
              ⌕
              <input
                value={query}
                onChange={(event) => setQuery(event.target.value)}
                placeholder="Search customers or contacts"
              />
            </label>
            <button className="filter-button">Status⌄</button>
            <button className="filter-button">Export ↓</button>
          </div>

          <div className="panel module-table">
            {filtered.length === 0 ? (
              <div className="state-panel">No customers match this search.</div>
            ) : (
              <>
                <div className="module-table-head">
                  <span>Customer</span>
                  <span>Contact</span>
                  <span>Type</span>
                  <span>Open value</span>
                  <span>Status</span>
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
                      <i className={`status-dot ${customer.status === 'Review' ? 'amber' : 'green'}`} />
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
  const { t } = useI18n()
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
          setError(reason instanceof Error ? reason.message : 'Quotes could not be loaded.')
          setLoading(false)
        }
      })
    return () => {
      ignore = true
    }
  }, [reloadKey])

  const filtered = useMemo(
    () => items.filter((quote) => `${quote.number} ${quote.customerId} ${quote.title}`.toLowerCase().includes(query.toLowerCase())),
    [items, query]
  )

  return (
    <div className="module-view">
      <div className="module-heading">
        <div>
          <p className="eyebrow">Commercial pipeline</p>
          <h1>Quotes</h1>
          <p className="heading-copy">Move from a clear offer to an accepted work order.</p>
        </div>
        <button className="primary-button" data-testid="02301-new-quote-btn" onClick={() => setShowModal(true)}>
          <span>+</span> New quote
        </button>
      </div>

      {loading && <div className="state-panel">Loading quotes…</div>}
      {error && <div className="state-panel error-state">{error}</div>}

      {!loading && !error && (
        <>
          <div className="quote-summary">
            <div>
              <small>Draft</small>
              <b>12</b>
            </div>
            <div>
              <small>Issued</small>
              <b>07</b>
            </div>
            <div>
              <small>Accepted this month</small>
              <b>18</b>
            </div>
            <div>
              <small>Conversion rate</small>
              <b>68%</b>
            </div>
          </div>

          <div className="module-toolbar">
            <label className="module-search">
              ⌕
              <input
                value={query}
                onChange={(event) => setQuery(event.target.value)}
                placeholder="Search quote number, customer or title"
              />
            </label>
            <button className="filter-button">All statuses⌄</button>
          </div>

          <div className="panel module-table">
            {filtered.length === 0 ? (
              <div className="state-panel">No quotes match this search.</div>
            ) : (
              <>
                <div className="module-table-head quote-head">
                  <span>Quote</span>
                  <span>Customer</span>
                  <span>Total</span>
                  <span>Status</span>
                  <span>Updated</span>
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
                    <span>₺{quote.total.toLocaleString('tr-TR')}</span>
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
                      {t[quote.state.charAt(0).toLowerCase() + quote.state.slice(1) as keyof typeof t] || quote.state}
                    </span>
                    <span>Recent</span>
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

import { useMemo, useState, useEffect } from 'react'
import './App.css'
import { AuthView } from './AuthView'
import { authApi, workOrdersApi, quotesApi, type AuthResult, type WorkOrder, type Quote } from './api'
import { CustomersView, QuotesView } from './ModuleViews'
import { InventoryView, PaymentsView, WorkOrdersView } from './OperationsViews'
import { CreateCustomerModal } from './CreateCustomerModal'
import { CreateQuoteModal } from './CreateQuoteModal'
import { CreateWorkOrderModal } from './CreateWorkOrderModal'
import { useI18n, formatCurrency } from './i18n'

function App() {
  const { lang, setLang, t, i18n } = useI18n()

  const [dashboardOrders, setDashboardOrders] = useState<WorkOrder[]>([])
  const [dashboardQuotes, setDashboardQuotes] = useState<Quote[]>([])
  const [session, setSession] = useState<AuthResult | null>(() => {
    const stored = localStorage.getItem('voltflow.session')
    return stored ? (JSON.parse(stored) as AuthResult) : null
  })
  const [activeView, setActiveView] = useState('Overview')
  
  useEffect(() => {
    if (session && activeView === 'Overview') {
      workOrdersApi.list().then(setDashboardOrders).catch(() => {})
      quotesApi.list().then(setDashboardQuotes).catch(() => {})
    }
  }, [session, activeView])

  const navigation = [
    { id: 'Overview', label: t.overview, icon: '⌂' },
    { id: 'Customers', label: t.customers, icon: '◌' },
    { id: 'Quotes', label: t.quotes, icon: '▤' },
    { id: 'Work orders', label: t.workOrders, icon: '↗', count: dashboardOrders.length > 0 ? dashboardOrders.length : undefined },
    { id: 'Inventory', label: t.inventory, icon: '▥' },
    { id: 'Payments', label: t.payments, icon: '₺' },
  ]

  const [search, setSearch] = useState('')
  const [showQuickCreate, setShowQuickCreate] = useState(false)
  const [quickAction, setQuickAction] = useState<'customer' | 'quote' | 'workorder' | null>(null)

  const filteredOrders = useMemo(() => {
    const query = search.toLowerCase().trim()
    if (!query) return dashboardOrders
    return dashboardOrders.filter((order) =>
      [order.number, order.title, order.customerId, order.assignedUserId || 'Unassigned'].some((value) =>
        value.toLowerCase().includes(query)
      )
    )
  }, [search, dashboardOrders])

  if (!session) return <AuthView onAuthenticated={setSession} />

  const handleLogout = async () => {
    if (session.token) {
      try {
        await authApi.revokeSession(session.token)
      } catch {
        // Ignored: proceed with client-side cleanup even if server is unreachable
      }
    }
    localStorage.removeItem('voltflow.session')
    setSession(null)
  }

  return (
    <div className="app-shell">
      <aside className="sidebar">
        <div className="brand-block">
          <div className="brand-mark">V</div>
          <div>
            <strong>voltflow</strong>
            <span>field operations</span>
          </div>
        </div>
        <div className="workspace-switcher">
          <span className="workspace-dot" />
          <div>
            <b>Voltflow Workspace</b>
            <small>{t.singleCompany}</small>
          </div>
          <span className="chevron">⌄</span>
        </div>
        <nav className="primary-nav" aria-label="Main navigation">
          <p className="nav-label">{t.workspace}</p>
          {navigation.map((item) => (
            <button
              className={`nav-item ${activeView === item.id ? 'active' : ''}`}
              key={item.id}
              onClick={() => setActiveView(item.id)}
            >
              <span className="nav-icon">{item.icon}</span>
              <span>{item.label}</span>
              {item.count && <em>{item.count}</em>}
            </button>
          ))}
          <p className="nav-label secondary-label">{t.system}</p>
          <button className="nav-item" onClick={() => setActiveView('Audit log')}>
            <span className="nav-icon">◫</span>
            <span>{t.auditLog}</span>
          </button>
          <button className="nav-item" onClick={() => setActiveView('Admin')}>
            <span className="nav-icon">⚙</span>
            <span>{t.admin}</span>
          </button>
        </nav>
        <div className="sidebar-footer">
          <div className="help-card">
            <span className="help-icon">?</span>
            <div>
              <b>{t.needAHand}</b>
              <small>{t.openGuide}</small>
            </div>
          </div>
          <button
            className="user-chip user-button"
            onClick={handleLogout}
            title={t.clickToLogout}
          >
            <div className="avatar">
              {session.name
                .split(' ')
                .map((part) => part[0])
                .join('')
                .slice(0, 2)}
            </div>
            <div>
              <b>{session.name}</b>
              <small>{t.employee}</small>
            </div>
            <span className="more">↪</span>
          </button>
        </div>
      </aside>

      <main className="main-content">
        <header className="topbar">
          <div className="breadcrumbs">
            <span>Workspace</span>
            <b>/</b>
            <strong>{activeView}</strong>
          </div>
          <div className="top-actions">
            <label className="search-box">
              <span>⌕</span>
              <input
                value={search}
                onChange={(event) => setSearch(event.target.value)}
                placeholder="Search work, customers, quotes..."
              />
              <kbd>⌘ K</kbd>
            </label>
            <button
              type="button"
              className="lang-switcher-btn"
              onClick={() => setLang(lang === 'en' ? 'tr' : 'en')}
              title={lang === 'en' ? 'Türkçe arayüze geç' : 'Switch to English (USA)'}
            >
              {lang === 'en' ? '🇺🇸 EN (US)' : '🇹🇷 TR'}
            </button>
            <button className="icon-button" title="Notifications">
              ♧<i />
            </button>
            <button className="icon-button" title="Settings">
              ⚙
            </button>
          </div>
        </header>

        {activeView === 'Customers' ? (
          <CustomersView />
        ) : activeView === 'Quotes' ? (
          <QuotesView />
        ) : activeView === 'Work orders' ? (
          <WorkOrdersView />
        ) : activeView === 'Inventory' ? (
          <InventoryView />
        ) : activeView === 'Payments' ? (
          <PaymentsView />
        ) : (
          <div className="content-wrap">
            <section className="page-heading">
              <div>
                <p className="eyebrow">
                  {new Date().toLocaleDateString(i18n.language === 'tr' ? 'tr-TR' : 'en-US', { weekday: 'long', day: 'numeric', month: 'long', year: 'numeric' })}
                </p>
                <h1>
                  {new Date().getHours() < 12 ? (i18n.language === 'tr' ? 'Günaydın' : 'Good morning') : new Date().getHours() < 18 ? (i18n.language === 'tr' ? 'İyi günler' : 'Good afternoon') : (i18n.language === 'tr' ? 'İyi akşamlar' : 'Good evening')}, {session.name.split(' ')[0]}.
                </h1>
                <p className="heading-copy">Here is what needs your attention across the operation today.</p>
              </div>
              <button className="primary-button" onClick={() => setShowQuickCreate(true)}>
                <span>+</span> Quick create
              </button>
            </section>

            <section className="metric-grid" aria-label="Operational summary">
              <article className="metric-card highlighted">
                <div className="metric-top">
                  <span>Open work orders</span>
                  <span className="metric-arrow">↗</span>
                </div>
                <strong>{dashboardOrders.filter(o => o.status === 'Open' || o.status === 'In progress').length}</strong>
                <p>
                  <b className="positive">-</b> from last week
                </p>
                <div className="sparkline warm"></div>
              </article>
              <article className="metric-card">
                <div className="metric-top">
                  <span>Pending quotes</span>
                  <span className="metric-arrow">↗</span>
                </div>
                <strong>{dashboardQuotes.filter(q => q.state === 'Issued').length}</strong>
                <p>
                  <b className="warning">-</b>
                </p>
                <div className="sparkline blue"></div>
              </article>
              <article className="metric-card">
                <div className="metric-top">
                  <span>Low stock items</span>
                  <span className="metric-arrow">↗</span>
                </div>
                <strong>0</strong>
                <p>
                  <b className="negative">-</b>
                </p>
                <div className="sparkline red"></div>
              </article>
              <article className="metric-card">
                <div className="metric-top">
                  <span>Receivables</span>
                  <span className="metric-arrow">↗</span>
                </div>
                <strong>{formatCurrency(0, i18n.language === 'tr' ? 'TRY' : 'USD')}</strong>
                <p>
                  <b className="positive">-</b> this month
                </p>
                <div className="sparkline green"></div>
              </article>
            </section>

            <section className="content-grid">
              <article className="panel work-panel">
                <div className="panel-heading">
                  <div>
                    <h2>Work orders</h2>
                    <p>Today’s operational queue</p>
                  </div>
                  <button className="text-button" onClick={() => setActiveView('Work orders')}>
                    View all <span>→</span>
                  </button>
                </div>
                <div className="table-tools">
                  <div className="mini-tabs">
                    <button className="selected">
                      All <b>{dashboardOrders.length}</b>
                    </button>
                    <button>
                      Mine <b>{dashboardOrders.filter(o => o.assignedUserId === session.userId).length}</b>
                    </button>
                    <button>
                      Unassigned <b>{dashboardOrders.filter(o => !o.assignedUserId).length}</b>
                    </button>
                  </div>
                  <button className="filter-button">☷ Filter</button>
                </div>
                <div className="work-table">
                  <div className="table-row table-head">
                    <span>Work order</span>
                    <span>Customer</span>
                    <span>Assignee</span>
                    <span>Status</span>
                  </div>
                  {filteredOrders.map((order) => (
                    <div className="table-row" key={order.id}>
                      <span>
                        <b className="order-number">{order.number}</b>
                        <strong>{order.title}</strong>
                      </span>
                      <span>{order.customerId}</span>
                      <span className="assignee">
                        <span className="tiny-avatar">
                          {!order.assignedUserId
                            ? '?'
                            : order.assignedUserId
                                .split(' ')
                                .map((part) => part[0])
                                .join('')}
                        </span>
                        {!order.assignedUserId ? t.unassigned : order.assignedUserId}
                      </span>
                      <span>
                        <i className={`status-dot ${order.status === 'Completed' ? 'green' : order.status === 'In progress' ? 'amber' : order.status === 'Assigned' ? 'blue' : 'slate'}`} />
                        {order.status === 'In progress' ? t.inProgress : order.status === 'On hold' ? t.onHold : t[order.status.toLowerCase() as keyof typeof t] || order.status}
                      </span>
                    </div>
                  ))}
                  {filteredOrders.length === 0 && (
                    <div className="empty-row">No work orders match your search.</div>
                  )}
                </div>
              </article>

              <article className="panel activity-panel">
                <div className="panel-heading">
                  <div>
                    <h2>Recent activity</h2>
                    <p>Last 24 hours</p>
                  </div>
                  <button className="icon-button small">•••</button>
                </div>
                <div className="activity-list">
                  <div className="empty-row">No recent activity.</div>
                </div>
                <button className="activity-footer">
                  Open audit timeline <span>→</span>
                </button>
              </article>
            </section>

            <section className="lower-grid">
              <article className="panel attention-panel">
                <div className="panel-heading">
                  <div>
                    <h2>Attention needed</h2>
                    <p>Items that need a decision</p>
                  </div>
                  <button className="text-button">
                    See all <span>→</span>
                  </button>
                </div>
                <div className="attention-list">
                  {dashboardQuotes.filter(q => q.state === 'Issued').length === 0 &&
                   dashboardOrders.filter(o => o.status === 'Open').length === 0 &&
                    <div className="empty-row">No attention needed right now.</div>
                  }
                  {dashboardQuotes.filter(q => q.state === 'Issued').length > 0 && (
                    <div>
                      <span className="attention-marker amber-marker" />
                      <div>
                        <b>{dashboardQuotes.filter(q => q.state === 'Issued').length} quotes are pending</b>
                        <small>Follow up with customers</small>
                      </div>
                      <button onClick={() => setActiveView('Quotes')}>Review →</button>
                    </div>
                  )}
                  {dashboardOrders.filter(o => o.status === 'Open').length > 0 && (
                    <div>
                      <span className="attention-marker blue-marker" />
                      <div>
                        <b>{dashboardOrders.filter(o => o.status === 'Open').length} unassigned work orders</b>
                        <small>Needs to be scheduled</small>
                      </div>
                      <button onClick={() => setActiveView('Work orders')}>Assign →</button>
                    </div>
                  )}
                </div>
              </article>

              <article className="panel pulse-panel">
                <div className="panel-heading">
                  <div>
                    <h2>Monthly pulse</h2>
                    <p>Revenue collected vs target</p>
                  </div>
                  <button className="filter-button">Sep 2026⌄</button>
                </div>
                <div className="pulse-value">
                  <strong>{formatCurrency(0, i18n.language === 'tr' ? 'TRY' : 'USD')}</strong>
                  <span>of {formatCurrency(500000, i18n.language === 'tr' ? 'TRY' : 'USD')} target</span>
                </div>
                <div className="progress-bar">
                  <span style={{ width: '0%' }} />
                </div>
                <div className="pulse-foot">
                  <span>0% collected</span>
                  <b>{formatCurrency(500000, i18n.language === 'tr' ? 'TRY' : 'USD')} remaining</b>
                </div>
              </article>
            </section>
          </div>
        )}
      </main>

      {showQuickCreate && (
        <div className="modal-backdrop" onClick={() => setShowQuickCreate(false)}>
          <div className="quick-modal" onClick={(event) => event.stopPropagation()}>
            <button className="modal-close" onClick={() => setShowQuickCreate(false)}>
              ×
            </button>
            <p className="eyebrow">Create something new</p>
            <h2>What are you working on?</h2>
            <div className="quick-options">
              <button
                onClick={() => {
                  setShowQuickCreate(false)
                  setQuickAction('workorder')
                }}
              >
                <span className="quick-icon amber-bg">↗</span>
                <b>Work order</b>
                <small>Start a new service job</small>
                <em>→</em>
              </button>
              <button
                onClick={() => {
                  setShowQuickCreate(false)
                  setQuickAction('quote')
                }}
              >
                <span className="quick-icon blue-bg">▤</span>
                <b>Quote</b>
                <small>Prepare an offer for a customer</small>
                <em>→</em>
              </button>
              <button
                onClick={() => {
                  setShowQuickCreate(false)
                  setQuickAction('customer')
                }}
              >
                <span className="quick-icon green-bg">◌</span>
                <b>Customer</b>
                <small>Add a customer or candidate</small>
                <em>→</em>
              </button>
            </div>
          </div>
        </div>
      )}

      <CreateCustomerModal
        isOpen={quickAction === 'customer'}
        onClose={() => setQuickAction(null)}
        onSuccess={() => setActiveView('Customers')}
      />
      <CreateQuoteModal
        isOpen={quickAction === 'quote'}
        onClose={() => setQuickAction(null)}
        onSuccess={() => setActiveView('Quotes')}
      />
      <CreateWorkOrderModal
        isOpen={quickAction === 'workorder'}
        onClose={() => setQuickAction(null)}
        onSuccess={() => setActiveView('Work orders')}
      />
    </div>
  )
}

export default App

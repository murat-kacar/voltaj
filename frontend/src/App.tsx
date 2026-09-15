import { useMemo, useState } from 'react'
import './App.css'
import { AuthView } from './AuthView'
import { authApi, type AuthResult } from './api'
import { CustomersView, QuotesView } from './ModuleViews'
import { InventoryView, PaymentsView, WorkOrdersView } from './OperationsViews'
import { CreateCustomerModal } from './CreateCustomerModal'
import { CreateQuoteModal } from './CreateQuoteModal'
import { CreateWorkOrderModal } from './CreateWorkOrderModal'
import { useI18n } from './i18n'

type WorkOrder = {
  id: string
  number: string
  title: string
  customer: string
  assignee: string
  status: 'In progress' | 'Assigned' | 'Open' | 'Completed' | 'On hold'
  due: string
  tone: string
}

const workOrders: WorkOrder[] = [
  { id: '1', number: 'WO-1048', title: 'Main panel replacement', customer: 'Arden Kitchens', assignee: 'M. Kaya', status: 'In progress', due: 'Today, 14:30', tone: 'amber' },
  { id: '2', number: 'WO-1047', title: 'Warehouse lighting inspection', customer: 'Northline Logistics', assignee: 'S. Demir', status: 'Assigned', due: 'Today, 16:00', tone: 'blue' },
  { id: '3', number: 'WO-1046', title: 'Boiler room maintenance', customer: 'Mavi Apart', assignee: 'Unassigned', status: 'Open', due: 'Tomorrow', tone: 'slate' },
  { id: '4', number: 'WO-1045', title: 'Emergency socket repair', customer: 'Bora Office', assignee: 'E. Aydin', status: 'Completed', due: 'Yesterday', tone: 'green' },
]

function App() {
  const { lang, setLang, t } = useI18n()

  const navigation = [
    { id: 'Overview', label: t.overview, icon: '⌂' },
    { id: 'Customers', label: t.customers, icon: '◌' },
    { id: 'Quotes', label: t.quotes, icon: '▤' },
    { id: 'Work orders', label: t.workOrders, icon: '↗', count: 8 },
    { id: 'Inventory', label: t.inventory, icon: '▥' },
    { id: 'Payments', label: t.payments, icon: '₺' },
  ]
  const [session, setSession] = useState<AuthResult | null>(() => {
    const stored = localStorage.getItem('voltflow.session')
    return stored ? (JSON.parse(stored) as AuthResult) : null
  })
  const [activeView, setActiveView] = useState('Overview')
  const [search, setSearch] = useState('')
  const [showQuickCreate, setShowQuickCreate] = useState(false)
  const [quickAction, setQuickAction] = useState<'customer' | 'quote' | 'workorder' | null>(null)

  const filteredOrders = useMemo(() => {
    const query = search.toLowerCase().trim()
    if (!query) return workOrders
    return workOrders.filter((order) =>
      [order.number, order.title, order.customer, order.assignee].some((value) =>
        value.toLowerCase().includes(query)
      )
    )
  }, [search])

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
            <b>Artemis Elektrik</b>
            <small>Single company workspace</small>
          </div>
          <span className="chevron">⌄</span>
        </div>
        <nav className="primary-nav" aria-label="Main navigation">
          <p className="nav-label">Workspace</p>
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
          <p className="nav-label secondary-label">System</p>
          <button className="nav-item" onClick={() => setActiveView('Audit log')}>
            <span className="nav-icon">◫</span>
            <span>Audit log</span>
          </button>
          <button className="nav-item" onClick={() => setActiveView('Admin')}>
            <span className="nav-icon">⚙</span>
            <span>Admin</span>
          </button>
        </nav>
        <div className="sidebar-footer">
          <div className="help-card">
            <span className="help-icon">?</span>
            <div>
              <b>Need a hand?</b>
              <small>Open the operations guide</small>
            </div>
          </div>
          <button
            className="user-chip user-button"
            onClick={handleLogout}
            title="Click to logout"
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
              <small>Employee</small>
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
                <p className="eyebrow">Monday, 11 September 2026</p>
                <h1>Good morning, Ayse.</h1>
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
                <strong>24</strong>
                <p>
                  <b className="positive">+12%</b> from last week
                </p>
                <div className="sparkline warm">
                  <i />
                  <i />
                  <i />
                  <i />
                  <i />
                  <i />
                  <i />
                </div>
              </article>
              <article className="metric-card">
                <div className="metric-top">
                  <span>Pending quotes</span>
                  <span className="metric-arrow">↗</span>
                </div>
                <strong>07</strong>
                <p>
                  <b className="warning">3 due today</b>
                </p>
                <div className="sparkline blue">
                  <i />
                  <i />
                  <i />
                  <i />
                  <i />
                  <i />
                  <i />
                </div>
              </article>
              <article className="metric-card">
                <div className="metric-top">
                  <span>Low stock items</span>
                  <span className="metric-arrow">↗</span>
                </div>
                <strong>05</strong>
                <p>
                  <b className="negative">2 critical</b>
                </p>
                <div className="sparkline red">
                  <i />
                  <i />
                  <i />
                  <i />
                  <i />
                  <i />
                  <i />
                </div>
              </article>
              <article className="metric-card">
                <div className="metric-top">
                  <span>Receivables</span>
                  <span className="metric-arrow">↗</span>
                </div>
                <strong>₺184.2k</strong>
                <p>
                  <b className="positive">+8.4%</b> this month
                </p>
                <div className="sparkline green">
                  <i />
                  <i />
                  <i />
                  <i />
                  <i />
                  <i />
                  <i />
                </div>
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
                      All <b>8</b>
                    </button>
                    <button>
                      Mine <b>3</b>
                    </button>
                    <button>
                      Unassigned <b>2</b>
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
                    <span>Due</span>
                  </div>
                  {filteredOrders.map((order) => (
                    <div className="table-row" key={order.id}>
                      <span>
                        <b className="order-number">{order.number}</b>
                        <strong>{order.title}</strong>
                      </span>
                      <span>{order.customer}</span>
                      <span className="assignee">
                        <span className="tiny-avatar">
                          {order.assignee === 'Unassigned'
                            ? '?'
                            : order.assignee
                                .split(' ')
                                .map((part) => part[0])
                                .join('')}
                        </span>
                        {order.assignee === 'Unassigned' ? t.unassigned : order.assignee}
                      </span>
                      <span>
                        <i className={`status-dot ${order.tone}`} />
                        {order.status === 'In progress' ? t.inProgress : order.status === 'On hold' ? t.onHold : t[order.status.toLowerCase() as keyof typeof t] || order.status}
                      </span>
                      <span className={order.due.includes('Today') ? 'due-today' : ''}>{order.due}</span>
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
                  <div className="activity-item">
                    <span className="activity-icon amber-bg">↗</span>
                    <div>
                      <p>
                        <b>Work order completed</b>
                      </p>
                      <small>WO-1045 · Bora Office</small>
                      <time>12 min ago</time>
                    </div>
                  </div>
                  <div className="activity-item">
                    <span className="activity-icon blue-bg">▤</span>
                    <div>
                      <p>
                        <b>Quote accepted</b>
                      </p>
                      <small>QT-208 · Arden Kitchens</small>
                      <time>48 min ago</time>
                    </div>
                  </div>
                  <div className="activity-item">
                    <span className="activity-icon green-bg">₺</span>
                    <div>
                      <p>
                        <b>Payment received</b>
                      </p>
                      <small>₺24,500 · Northline Logistics</small>
                      <time>2 hr ago</time>
                    </div>
                  </div>
                  <div className="activity-item">
                    <span className="activity-icon red-bg">!</span>
                    <div>
                      <p>
                        <b>Low stock alert</b>
                      </p>
                      <small>Copper cable 4mm · 8 left</small>
                      <time>3 hr ago</time>
                    </div>
                  </div>
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
                  <div>
                    <span className="attention-marker amber-marker" />
                    <div>
                      <b>3 quotes expire today</b>
                      <small>Review before 18:00</small>
                    </div>
                    <button onClick={() => setActiveView('Quotes')}>Review →</button>
                  </div>
                  <div>
                    <span className="attention-marker red-marker" />
                    <div>
                      <b>2 critical stock items</b>
                      <small>Reorder to avoid delays</small>
                    </div>
                    <button onClick={() => setActiveView('Inventory')}>Open inventory →</button>
                  </div>
                  <div>
                    <span className="attention-marker blue-marker" />
                    <div>
                      <b>4 users awaiting approval</b>
                      <small>Admin action required</small>
                    </div>
                    <button onClick={() => setActiveView('Admin')}>Review users →</button>
                  </div>
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
                  <strong>₺412.8k</strong>
                  <span>of ₺500k target</span>
                </div>
                <div className="progress-bar">
                  <span />
                </div>
                <div className="pulse-foot">
                  <span>82.6% collected</span>
                  <b>₺87.2k remaining</b>
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

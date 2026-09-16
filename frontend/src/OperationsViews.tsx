import { useEffect, useMemo, useState } from 'react'
import { workOrdersApi, type WorkOrder as ApiWorkOrder } from './api'
import { CreateWorkOrderModal } from './CreateWorkOrderModal'
import { WorkOrderDetailDrawer } from './WorkOrderDetailDrawer'
import { useTranslation, formatCurrency } from './i18n'

export function WorkOrdersView() {
  const { t, i18n } = useTranslation(['workOrders', 'common'])
  const [tab, setTab] = useState('All')
  const [query, setQuery] = useState('')
  const [rawOrders, setRawOrders] = useState<ApiWorkOrder[] | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')
  const [showModal, setShowModal] = useState(false)
  const [selectedOrder, setSelectedOrder] = useState<ApiWorkOrder | null>(null)
  const [reloadKey, setReloadKey] = useState(0)

  useEffect(() => {
    let ignore = false
    workOrdersApi
      .list()
      .then((items) => {
        if (!ignore) {
          setRawOrders(items)
          setError('')
          setLoading(false)
        }
      })
      .catch((reason: unknown) => {
        if (!ignore) {
          setError(reason instanceof Error ? reason.message : t('workOrders:errors.loadFailed'))
          setLoading(false)
        }
      })
    return () => {
      ignore = true
    }
  }, [reloadKey, t])

  const sourceOrders = useMemo(() => rawOrders ?? [], [rawOrders])
  const filtered = useMemo(
    () =>
      sourceOrders
        .filter((order) =>
          tab === 'Mine'
            ? order.assignedUserId === 'Ayse Kaya'
            : tab === 'Unassigned'
            ? !order.assignedUserId
            : true
        )
        .filter((order) =>
          `${order.number} ${order.title} ${order.customerId}`.toLowerCase().includes(query.toLowerCase())
        ),
    [query, tab, sourceOrders]
  )

  const getStatusLabel = (status: string) => {
    const keyMap: Record<string, string> = {
      'In progress': 'inProgress',
      'On hold': 'onHold',
      'Assigned': 'assigned',
      'Open': 'open',
      'Completed': 'completed',
      'Cancelled': 'cancelled',
      'Invoiced': 'invoiced',
    }
    const key = keyMap[status] || status.toLowerCase()
    const transKey = `common:status.${key}`
    return i18n.exists(transKey) ? t(transKey as any) : status
  }

  return (
    <div className="module-view">
      <div className="module-heading">
        <div>
          <p className="eyebrow">{t('workOrders:eyebrow')}</p>
          <h1>{t('workOrders:title')}</h1>
          <p className="heading-copy">{t('workOrders:subtitle')}</p>
        </div>
        <button className="primary-button" data-testid="03101-new-work-order-btn" onClick={() => setShowModal(true)}>
          <span>+</span> {t('workOrders:newOrder')}
        </button>
      </div>

      {loading && <div className="state-panel">{t('common:tables.loading')}</div>}
      {error && (
        <div className="state-panel error-state">
          {error}
          <button className="text-button" onClick={() => setReloadKey((k) => k + 1)}>
            {t('common:actions.refresh')} →
          </button>
        </div>
      )}

      {!loading && !error && (
        <>
          <div className="quote-summary">
            <div>
              <small>{t('common:status.open')}</small>
              <b>{sourceOrders.filter((o) => o.status === 'Open').length}</b>
            </div>
            <div>
              <small>{t('common:status.inProgress')}</small>
              <b>{sourceOrders.filter((o) => o.status === 'In progress').length}</b>
            </div>
            <div>
              <small>{t('common:status.unassigned')}</small>
              <b>{sourceOrders.filter((o) => !o.assignedUserId).length}</b>
            </div>
            <div>
              <small>{t('workOrders:tabs.all')}</small>
              <b>{sourceOrders.length}</b>
            </div>
          </div>

          <div className="module-toolbar">
            <div className="mini-tabs">
              <button className={tab === 'All' ? 'selected' : ''} onClick={() => setTab('All')}>
                {t('workOrders:tabs.all')} <b>{sourceOrders.length}</b>
              </button>
              <button className={tab === 'Mine' ? 'selected' : ''} onClick={() => setTab('Mine')}>
                {t('workOrders:tabs.mine')} <b>{sourceOrders.filter((o) => o.assignedUserId === 'Ayse Kaya').length}</b>
              </button>
              <button className={tab === 'Unassigned' ? 'selected' : ''} onClick={() => setTab('Unassigned')}>
                {t('workOrders:tabs.unassigned')} <b>{sourceOrders.filter((o) => !o.assignedUserId).length}</b>
              </button>
            </div>
            <label className="module-search">
              ⌕
              <input
                value={query}
                onChange={(event) => setQuery(event.target.value)}
                placeholder={t('workOrders:searchPlaceholder')}
              />
            </label>
            <button className="filter-button">{t('common:actions.filter')}⌄</button>
          </div>

          <div className="panel module-table">
            {filtered.length === 0 ? (
              <div className="state-panel">{t('common:tables.noData')}</div>
            ) : (
              <>
                <div className="module-table-head work-module-head">
                  <span>{t('workOrders:table.order')}</span>
                  <span>{t('workOrders:table.customer')}</span>
                  <span>{t('workOrders:table.assigned')}</span>
                  <span>{t('workOrders:table.status')}</span>
                  <span>{t('workOrders:table.total')}</span>
                </div>
                {filtered.map((order) => (
                  <div
                    className="module-table-row work-module-row clickable-row"
                    key={order.number}
                    onClick={() => setSelectedOrder(order)}
                    title={t('common:actions.details')}
                  >
                    <span>
                      <b>{order.number}</b>
                      <small>{order.title}</small>
                    </span>
                    <span>{order.customerId}</span>
                    <span>{order.assignedUserId ?? t('common:status.unassigned')}</span>
                    <span>
                      <i
                        className={`status-dot ${
                          order.status === 'Completed'
                            ? 'green'
                            : order.status === 'In progress'
                            ? 'amber'
                            : order.status === 'Assigned'
                            ? 'blue'
                            : 'slate'
                        }`}
                      />
                      {getStatusLabel(order.status)}
                    </span>
                    <span>{formatCurrency(order.total, i18n.language === 'tr' ? 'TRY' : 'USD')}</span>
                  </div>
                ))}
              </>
            )}
          </div>
        </>
      )}

      <CreateWorkOrderModal
        isOpen={showModal}
        onClose={() => setShowModal(false)}
        onSuccess={() => setReloadKey((k) => k + 1)}
      />

      <WorkOrderDetailDrawer
        order={selectedOrder}
        isOpen={Boolean(selectedOrder)}
        onClose={() => setSelectedOrder(null)}
        onUpdated={() => setReloadKey((k) => k + 1)}
      />
    </div>
  )
}

export function InventoryView() {
  const items: any[] = []
  return (
    <div className="module-view">
      <div className="module-heading">
        <div>
          <p className="eyebrow">Materials & stock</p>
          <h1>Inventory</h1>
          <p className="heading-copy">Know what is available before the next job starts.</p>
        </div>
        <button className="primary-button">
          <span>+</span> Adjust stock
        </button>
      </div>
      <div className="quote-summary">
        <div>
          <small>Total materials</small>
          <b>0</b>
        </div>
        <div>
          <small>Critical items</small>
          <b>0</b>
        </div>
        <div>
          <small>Reserved</small>
          <b>₺0</b>
        </div>
        <div>
          <small>Stock value</small>
          <b>₺0</b>
        </div>
      </div>
      <div className="panel module-table">
        <div className="module-table-head inventory-head">
          <span>Material</span>
          <span>Code</span>
          <span>Available</span>
          <span>Status</span>
          <span>Value</span>
        </div>
        {items.length === 0 && (
          <div className="empty-row" style={{ padding: '24px', textAlign: 'center', color: 'var(--text-tertiary)' }}>No materials found.</div>
        )}
        {items.map((item) => (
          <div className="module-table-row inventory-row" key={item[1]}>
            <span>
              <b>{item[0]}</b>
              <small>{item[1]}</small>
            </span>
            <span>{item[1]}</span>
            <span>{item[2]}</span>
            <span>
              <i className={`status-dot ${item[3] === 'Critical' ? 'red' : 'green'}`} />
              {item[3]}
            </span>
            <span>{item[4]}</span>
          </div>
        ))}
      </div>
    </div>
  )
}

export function PaymentsView() {
  return (
    <div className="module-view">
      <div className="module-heading">
        <div>
          <p className="eyebrow">Cash & receivables</p>
          <h1>Payments</h1>
          <p className="heading-copy">Track incoming money and what remains open.</p>
        </div>
        <button className="primary-button">
          <span>+</span> Record payment
        </button>
      </div>
      <div className="quote-summary">
        <div>
          <small>Collected this month</small>
          <b>₺0</b>
        </div>
        <div>
          <small>Outstanding</small>
          <b>₺0</b>
        </div>
        <div>
          <small>Due this week</small>
          <b>₺0</b>
        </div>
        <div>
          <small>Allocated</small>
          <b>0%</b>
        </div>
      </div>
      <div className="panel payment-callout">
        <span className="activity-icon green-bg">₺</span>
        <div>
          <b>Payment ledger is connected</b>
          <p>New receipts and invoice allocations will appear here with their operation history.</p>
        </div>
        <button className="text-button">Open ledger →</button>
      </div>
    </div>
  )
}

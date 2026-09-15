import { useEffect, useMemo, useState } from 'react'
import { workOrdersApi, type WorkOrder as ApiWorkOrder } from './api'
import { CreateWorkOrderModal } from './CreateWorkOrderModal'
import { WorkOrderDetailDrawer } from './WorkOrderDetailDrawer'
import { useI18n } from './i18n'

export function WorkOrdersView() {
  const { t } = useI18n()
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
          setError(reason instanceof Error ? reason.message : 'Work orders could not be loaded.')
          setLoading(false)
        }
      })
    return () => {
      ignore = true
    }
  }, [reloadKey])

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

  return (
    <div className="module-view">
      <div className="module-heading">
        <div>
          <p className="eyebrow">Field operations</p>
          <h1>Work orders</h1>
          <p className="heading-copy">Keep the queue moving and make ownership obvious.</p>
        </div>
        <button className="primary-button" data-testid="03101-new-work-order-btn" onClick={() => setShowModal(true)}>
          <span>+</span> New work order
        </button>
      </div>

      {loading && <div className="state-panel">Loading work orders…</div>}
      {error && (
        <div className="state-panel error-state">
          {error}
          <button className="text-button" onClick={() => setReloadKey((k) => k + 1)}>
            Retry →
          </button>
        </div>
      )}

      {!loading && !error && (
        <>
          <div className="quote-summary">
            <div>
              <small>Open</small>
              <b>{sourceOrders.filter((o) => o.status === 'Open').length}</b>
            </div>
            <div>
              <small>In progress</small>
              <b>{sourceOrders.filter((o) => o.status === 'In progress').length}</b>
            </div>
            <div>
              <small>Unassigned</small>
              <b>{sourceOrders.filter((o) => !o.assignedUserId).length}</b>
            </div>
            <div>
              <small>Total orders</small>
              <b>{sourceOrders.length}</b>
            </div>
          </div>

          <div className="module-toolbar">
            <div className="mini-tabs">
              <button className={tab === 'All' ? 'selected' : ''} onClick={() => setTab('All')}>
                All <b>{sourceOrders.length}</b>
              </button>
              <button className={tab === 'Mine' ? 'selected' : ''} onClick={() => setTab('Mine')}>
                Mine <b>{sourceOrders.filter((o) => o.assignedUserId === 'Ayse Kaya').length}</b>
              </button>
              <button className={tab === 'Unassigned' ? 'selected' : ''} onClick={() => setTab('Unassigned')}>
                Unassigned <b>{sourceOrders.filter((o) => !o.assignedUserId).length}</b>
              </button>
            </div>
            <label className="module-search">
              ⌕
              <input
                value={query}
                onChange={(event) => setQuery(event.target.value)}
                placeholder="Search work orders"
              />
            </label>
            <button className="filter-button">Filter⌄</button>
          </div>

          <div className="panel module-table">
            {filtered.length === 0 ? (
              <div className="state-panel">No work orders match this view.</div>
            ) : (
              <>
                <div className="module-table-head work-module-head">
                  <span>Work order</span>
                  <span>Customer</span>
                  <span>Assignee</span>
                  <span>Status</span>
                  <span>Due</span>
                  <span>Value</span>
                </div>
                {filtered.map((order) => (
                  <div
                    className="module-table-row work-module-row clickable-row"
                    key={order.number}
                    onClick={() => setSelectedOrder(order)}
                    title="Click to view details in drawer"
                  >
                    <span>
                      <b>{order.number}</b>
                      <small>{order.title}</small>
                    </span>
                    <span>{order.customerId}</span>
                    <span>{order.assignedUserId ?? t.unassigned}</span>
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
                      {order.status === 'In progress' ? t.inProgress : order.status === 'On hold' ? t.onHold : t[order.status.toLowerCase() as keyof typeof t] || order.status}
                    </span>
                    <span>Not scheduled</span>
                    <span>₺{order.total.toLocaleString('tr-TR')}</span>
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
  const items = [
    ['Copper cable 4mm', 'MAT-004', '8 m', 'Critical', '₺2,400'],
    ['Industrial breaker 32A', 'MAT-018', '24 pcs', 'Healthy', '₺18,600'],
    ['LED panel 60x60', 'MAT-031', '42 pcs', 'Healthy', '₺35,700'],
  ]
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
          <b>128</b>
        </div>
        <div>
          <small>Critical items</small>
          <b>05</b>
        </div>
        <div>
          <small>Reserved</small>
          <b>₺48.2k</b>
        </div>
        <div>
          <small>Stock value</small>
          <b>₺412k</b>
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
        {items.map((item) => (
          <div className="module-table-row inventory-row" key={item[1]}>
            <span>
              <b>{item[0]}</b>
              <small>Warehouse A · electrical</small>
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
          <b>₺184.2k</b>
        </div>
        <div>
          <small>Outstanding</small>
          <b>₺96.8k</b>
        </div>
        <div>
          <small>Due this week</small>
          <b>₺34.2k</b>
        </div>
        <div>
          <small>Allocated</small>
          <b>82%</b>
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

import { useState } from 'react'
import { Drawer } from './Drawer'
import { ApiError, workOrdersApi, type ProblemDetails, type WorkOrder } from './api'

type Props = {
  order: WorkOrder | null
  isOpen: boolean
  onClose: () => void
  onUpdated: () => void
}

export function WorkOrderDetailDrawer({ order, isOpen, onClose, onUpdated }: Props) {
  const [loading, setLoading] = useState(false)
  const [addingItem, setAddingItem] = useState(false)
  const [problem, setProblem] = useState<ProblemDetails | null>(null)
  
  // Proof of work states
  const [isCompleting, setIsCompleting] = useState(false)
  const [signatureData, setSignatureData] = useState<string>('')
  const [photoUrl, setPhotoUrl] = useState<string>('')

  // Material states
  const [mDesc, setMDesc] = useState('')
  const [mQty, setMQty] = useState(1)
  const [mPrice, setMPrice] = useState(0)
  
  const [checkingIn, setCheckingIn] = useState(false)

  if (!order) return null

  const resetStates = () => {
    setLoading(false)
    setAddingItem(false)
    setProblem(null)
    setIsCompleting(false)
    setSignatureData('')
    setPhotoUrl('')
    setMDesc('')
    setMQty(1)
    setMPrice(0)
  }

  const handleClose = () => {
    resetStates()
    onClose()
  }

  const handleCompleteSubmit = async () => {
    if (!signatureData && !photoUrl) {
      setProblem({ title: 'Validation', detail: 'Either a signature or a photo is required to complete the work order.' })
      return
    }

    setLoading(true)
    setProblem(null)
    try {
      await workOrdersApi.complete(order.id, signatureData, photoUrl)
      onUpdated()
      handleClose()
    } catch (err: unknown) {
      handleApiError(err, 'Completion failed')
    } finally {
      setLoading(false)
    }
  }

  const handleAddItem = async (e: React.FormEvent) => {
    e.preventDefault()
    if (!mDesc || mQty <= 0 || mPrice < 0) return

    setAddingItem(true)
    setProblem(null)
    try {
      await workOrdersApi.addItem(order.id, mDesc, mQty, mPrice)
      setMDesc('')
      setMQty(1)
      setMPrice(0)
      onUpdated() // Refresh order
    } catch (err: unknown) {
      handleApiError(err, 'Failed to add item')
    } finally {
      setAddingItem(false)
    }
  }

  const handleCheckIn = async () => {
    setCheckingIn(true)
    setProblem(null)
    try {
      await workOrdersApi.checkIn(order.id)
      onUpdated()
    } catch (err: unknown) {
      handleApiError(err, 'Check-in failed')
    } finally {
      setCheckingIn(false)
    }
  }

  const handleApiError = (err: unknown, defaultTitle: string) => {
    if (err instanceof ApiError && err.problemDetails) {
      setProblem(err.problemDetails)
    } else if (err instanceof Error) {
      setProblem({ title: defaultTitle, detail: err.message })
    } else {
      setProblem({ title: 'Error', detail: 'An unexpected error occurred.' })
    }
  }

  const statusClass =
    order.status === 'Completed'
      ? 'completed'
      : order.status === 'In progress'
      ? 'inprogress'
      : order.status === 'Assigned'
      ? 'assigned'
      : 'open'

  // --- Proof of Work View ---
  if (isCompleting) {
    return (
      <Drawer
        isOpen={isOpen}
        onClose={handleClose}
        eyebrow="Proof of Work"
        title="Complete Work Order"
        footer={
          <>
            <button className="secondary-button" onClick={() => setIsCompleting(false)} disabled={loading}>
              Back
            </button>
            <button className="primary-button" onClick={handleCompleteSubmit} disabled={loading || (!signatureData && !photoUrl)}>
              {loading ? 'Submitting…' : 'Confirm & Complete'}
            </button>
          </>
        }
      >
        {problem && (
          <div className="problem-details" style={{ marginBottom: 16 }}>
            <strong>{problem.title ?? 'Action Failed'}</strong>
            <span>{problem.detail}</span>
          </div>
        )}

        <div className="detail-card" style={{ display: 'flex', flexDirection: 'column', gap: '16px' }}>
          <div>
            <label style={{ display: 'block', fontSize: 12, fontWeight: 600, marginBottom: 8, color: 'var(--ink)' }}>
              Signature Capture
            </label>
            <div 
              data-testid="03401-signature-box"
              onClick={() => setSignatureData(signatureData ? '' : 'base64:mock_signature_data_xyz')}
              style={{
                height: 120,
                border: `2px dashed ${signatureData ? 'var(--brand)' : 'var(--border)'}`,
                borderRadius: 8,
                display: 'flex',
                alignItems: 'center',
                justifyContent: 'center',
                cursor: 'pointer',
                backgroundColor: signatureData ? 'rgba(0, 211, 127, 0.05)' : '#fafafa',
                transition: 'all 0.2s',
                color: signatureData ? 'var(--brand)' : '#666',
                fontWeight: 500,
                fontSize: 14
              }}
            >
              {signatureData ? '✓ Signature Captured (Click to clear)' : 'Click to sign (Mock)'}
            </div>
          </div>

          <div>
            <label style={{ display: 'block', fontSize: 12, fontWeight: 600, marginBottom: 8, color: 'var(--ink)' }}>
              Work Photo
            </label>
            <div 
              data-testid="03401-photo-box"
              onClick={() => setPhotoUrl(photoUrl ? '' : 'https://mock-storage.com/photo_123.jpg')}
              style={{
                height: 120,
                border: `2px dashed ${photoUrl ? 'var(--brand)' : 'var(--border)'}`,
                borderRadius: 8,
                display: 'flex',
                alignItems: 'center',
                justifyContent: 'center',
                cursor: 'pointer',
                backgroundColor: photoUrl ? 'rgba(0, 211, 127, 0.05)' : '#fafafa',
                transition: 'all 0.2s',
                color: photoUrl ? 'var(--brand)' : '#666',
                fontWeight: 500,
                fontSize: 14
              }}
            >
              {photoUrl ? '✓ Photo Uploaded (Click to clear)' : 'Click to upload photo (Mock)'}
            </div>
          </div>
        </div>
      </Drawer>
    )
  }

  // --- Normal View ---
  return (
    <Drawer
      isOpen={isOpen}
      onClose={handleClose}
      eyebrow="Work Order Details"
      title={`${order.number} — ${order.title}`}
      footer={
        <>
          <button className="secondary-button" onClick={handleClose} disabled={loading || addingItem}>
            Close
          </button>
          {order.status !== 'Completed' && (
            <button
              className="primary-button"
              data-testid="03401-open-complete-btn"
              onClick={() => setIsCompleting(true)}
              disabled={loading || addingItem}
            >
              ✓ Mark Completed
            </button>
          )}
        </>
      }
    >
      {problem && (
        <div className="problem-details" style={{ marginBottom: 16 }}>
          <strong>{problem.title ?? 'Action Failed'}</strong>
          <span>{problem.detail}</span>
          {problem.traceId && <small>Trace: {problem.traceId}</small>}
        </div>
      )}

      <div className="detail-card">
        <div className="detail-grid">
          <div className="detail-field">
            <small>Status</small>
            <span className={`badge ${statusClass}`}>{order.status}</span>
          </div>
          <div className="detail-field">
            <small>Total Value</small>
            <b>₺{order.total.toLocaleString('tr-TR')}</b>
          </div>
          <div className="detail-field">
            <small>Customer ID</small>
            <span style={{ fontSize: 11 }}>{order.customerId}</span>
          </div>
          <div className="detail-field">
            <small>Assigned Tech</small>
            <span style={{ fontSize: 11 }}>{order.assignedUserId ?? 'Unassigned'}</span>
          </div>
        </div>
      </div>

      <div className="detail-card">
        <div className="detail-field">
          <small>Job Description</small>
          <p style={{ margin: '4px 0 0', fontSize: '13px', lineHeight: 1.5, color: 'var(--ink)' }}>
            {order.title}
          </p>
        </div>
      </div>

      {order.status === 'Completed' && (order.signatureData || order.proofOfWorkPhotoUrl) && (
        <div className="detail-card">
          <div className="detail-field">
            <small>Proof of Work</small>
            <div style={{ display: 'flex', gap: 8, marginTop: 8 }}>
              {order.signatureData && <span className="badge inprogress">Signature attached</span>}
              {order.proofOfWorkPhotoUrl && <span className="badge assigned">Photo attached</span>}
            </div>
          </div>
        </div>
      )}

      {(order.checkInTime || order.checkOutTime) && (
        <div className="detail-card">
          <div className="detail-grid">
            {order.checkInTime && (
              <div className="detail-field">
                <small>Check-In Time</small>
                <span style={{ fontSize: 12 }}>{new Date(order.checkInTime).toLocaleString()}</span>
              </div>
            )}
            {order.checkOutTime && (
              <div className="detail-field">
                <small>Check-Out Time</small>
                <span style={{ fontSize: 12 }}>{new Date(order.checkOutTime).toLocaleString()}</span>
              </div>
            )}
          </div>
        </div>
      )}

      {order.status === 'In progress' && (
        <div className="detail-card">
          <div className="detail-field" style={{ marginBottom: 16 }}>
            <small>Time Tracking</small>
            {!order.checkInTime ? (
              <button 
                data-testid="03201-checkin-btn"
                onClick={handleCheckIn} 
                disabled={checkingIn} 
                className="secondary-button" 
                style={{ marginTop: 8, width: '100%', borderColor: 'var(--brand)', color: 'var(--brand)' }}>
                {checkingIn ? 'Checking in...' : '📍 Check-In (On Site)'}
              </button>
            ) : (
              <div style={{ marginTop: 8, fontSize: 12, color: 'var(--ink)' }}>
                Checked in at {new Date(order.checkInTime).toLocaleTimeString()}
              </div>
            )}
          </div>
          <hr style={{ border: 'none', borderTop: '1px solid var(--border)', margin: '12px 0' }} />
          <div className="detail-field">
            <small>Field Consumption (Add Material)</small>
            <form onSubmit={handleAddItem} style={{ display: 'flex', flexDirection: 'column', gap: 8, marginTop: 12 }}>
              <input
                className="input-field"
                placeholder="Material Description (e.g. Copper Wire)"
                value={mDesc}
                onChange={(e) => setMDesc(e.target.value)}
                required
              />
              <div style={{ display: 'flex', gap: 8 }}>
                <input
                  className="input-field"
                  type="number"
                  min="0.1"
                  step="0.1"
                  placeholder="Qty"
                  value={mQty || ''}
                  onChange={(e) => setMQty(parseFloat(e.target.value))}
                  required
                  style={{ flex: 1 }}
                />
                <input
                  className="input-field"
                  type="number"
                  min="0"
                  step="0.01"
                  placeholder="Unit Price"
                  value={mPrice || ''}
                  onChange={(e) => setMPrice(parseFloat(e.target.value))}
                  required
                  style={{ flex: 1 }}
                />
              </div>
              <button
                type="submit"
                className="secondary-button"
                style={{ alignSelf: 'flex-start', padding: '6px 12px', fontSize: 13, minHeight: 32 }}
                disabled={addingItem}
              >
                {addingItem ? 'Adding...' : '+ Add'}
              </button>
            </form>
          </div>
        </div>
      )}

      <div className="detail-card">
        <div className="detail-field">
          <small>Audit & Trace Lineage</small>
          <span style={{ fontSize: '11px', color: '#6d7f7a', fontFamily: 'monospace' }}>
            ID: {order.id}
          </span>
        </div>
      </div>
    </Drawer>
  )
}

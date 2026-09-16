import { useEffect, useRef, type ReactNode } from 'react'
import { useTranslation } from './i18n'

type DrawerProps = {
  isOpen: boolean
  onClose: () => void
  eyebrow?: string
  title: string
  children: ReactNode
  footer?: ReactNode
}

export function Drawer({ isOpen, onClose, eyebrow, title, children, footer }: DrawerProps) {
  const { t } = useTranslation('common')
  const panelRef = useRef<HTMLDivElement>(null)

  useEffect(() => {
    if (!isOpen) return

    // Scroll lock
    const originalOverflow = document.body.style.overflow
    document.body.style.overflow = 'hidden'

    // ESC key listener
    const handleKeyDown = (e: KeyboardEvent) => {
      if (e.key === 'Escape') {
        onClose()
      }
    }

    window.addEventListener('keydown', handleKeyDown)

    return () => {
      document.body.style.overflow = originalOverflow
      window.removeEventListener('keydown', handleKeyDown)
    }
  }, [isOpen, onClose])

  if (!isOpen) return null

  return (
    <div className="drawer-backdrop" onClick={onClose} role="dialog" aria-modal="true">
      <div
        className="drawer-panel"
        ref={panelRef}
        onClick={(e) => e.stopPropagation()}
      >
        <div className="drawer-header">
          <div>
            {eyebrow && <p className="eyebrow">{eyebrow}</p>}
            <h2>{title}</h2>
          </div>
          <button className="modal-close" onClick={onClose} title={t('actions.close')}>
            ×
          </button>
        </div>

        <div className="drawer-content">{children}</div>

        {footer && <div className="drawer-footer">{footer}</div>}
      </div>
    </div>
  )
}

import { useState, type KeyboardEvent, type ReactNode } from 'react'
import { Alert, Button, CircularProgress, Dialog, DialogActions, DialogContent, DialogTitle, Stack } from '@mui/material'
import { useI18n } from '../i18n'
import { errorText } from './errors'

type Props = {
  title: string
  submitLabel: string
  /** False keeps the submit button off until the form can be sent. */
  canSubmit?: boolean
  color?: 'primary' | 'error' | 'warning'
  maxWidth?: 'xs' | 'sm' | 'md' | 'lg'
  /** Enter in a text field sends the form. A form with many fields, where Enter is more likely to mean "next", turns it off. */
  submitOnEnter?: boolean
  fullScreen?: boolean
  onClose: () => void
  /** Sends the form. A failure is shown in the dialog and the user can try again; on success the caller closes the dialog. */
  onSubmit: () => Promise<void>
  children?: ReactNode
}

/** A dialog with a form or a question: it shows that it is working, shows what went wrong, and sends on Enter. */
export function FormDialog({ title, submitLabel, canSubmit = true, color = 'primary', maxWidth = 'xs', submitOnEnter = true, fullScreen = false, onClose, onSubmit, children }: Props) {
  const { translate: t } = useI18n()
  const [busy, setBusy] = useState(false)
  const [error, setError] = useState('')

  const submit = async () => {
    if (busy || !canSubmit) return
    setBusy(true)
    setError('')
    try {
      await onSubmit()
    } catch (reason) {
      setError(errorText(reason))
    } finally {
      setBusy(false)
    }
  }

  // Enter in a text field sends the form (an option list uses Enter to choose, so it is left alone)
  const handleKeyDown = (event: KeyboardEvent) => {
    const target = event.target
    if (submitOnEnter && event.key === 'Enter' && target instanceof HTMLInputElement && target.getAttribute('role') !== 'combobox') {
      event.preventDefault()
      void submit()
    }
  }

  return (
    <Dialog open onClose={busy ? undefined : onClose} maxWidth={maxWidth} fullWidth fullScreen={fullScreen} data-testid="dialog-163748">
      <DialogTitle>{title}</DialogTitle>
      <DialogContent onKeyDown={handleKeyDown}>
        <Stack spacing={2} sx={{ mt: 1 }}>
          {children}
          {error && <Alert severity="error">{error}</Alert>}
        </Stack>
      </DialogContent>
      <DialogActions>
        <Button onClick={onClose} disabled={busy} data-testid="button-a93a13">{t('common:actions.cancel')}</Button>
        <Button variant="contained" color={color} disabled={busy || !canSubmit} onClick={() => void submit()} data-testid="button-67deb1">
          {busy ? <CircularProgress size={22} /> : submitLabel}
        </Button>
      </DialogActions>
    </Dialog>
  )
}

import { Chip } from '@mui/material'
import type { QuoteState } from '../api'
import { useI18n } from '../i18n'
import { displayState, stateColor } from './quoteMath'

/** Where a quote stands, as a coloured tag. An issued quote past its last day says so. */
export function QuoteStateChip({ state, validUntil }: { state: QuoteState; validUntil: string | null }) {
  const { translate: t } = useI18n()
  const shown = displayState(state, validUntil)
  return <Chip size="small" color={stateColor(shown)} variant={shown === 'Draft' ? 'outlined' : 'filled'} label={t(`quotes:state.${shown}`)} />
}

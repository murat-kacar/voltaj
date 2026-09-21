import { useEffect, useMemo, useState } from 'react'
import { Alert, Box, Chip, CircularProgress } from '@mui/material'
import { cashShiftsApi, sessionRoles, type ShiftReport } from './api'
import { HistoryTab } from './HistoryTab'
import { PosTab } from './PosTab'
import { ShiftTab } from './ShiftTab'
import { formatDate } from './i18n/formatters'
import { useI18n } from './i18n'
import { errorText } from './common/errors'
import { PageHeader } from './common/PageHeader'

export type QuickSaleSection = 'sell' | 'history' | 'shift'

const TITLE_KEY = {
  sell: 'common:nav.quickSale',
  history: 'common:nav.saleHistory',
  shift: 'common:nav.cashShift',
} as const satisfies Record<QuickSaleSection, string>

type Props = {
  section: QuickSaleSection
  /** The register and the shift are separate menu entries; this moves between them. */
  onNavigate: (view: 'quick-sale' | 'cash-shift') => void
}

/** Quick sale: the register for door-to-door sales, its history and the cashier's shift, each on its own page. */
export function QuickSaleView({ section, onNavigate }: Props) {
  const { translate: t, lang } = useI18n()
  const isManager = useMemo(() => sessionRoles().some((role) => role === 'Admin' || role === 'Manager'), [])
  // undefined while the shift is being looked up, null when the user has none open
  const [report, setReport] = useState<ShiftReport | null | undefined>(undefined)
  const [error, setError] = useState('')

  const [reloadKey, setReloadKey] = useState(0)
  const reloadShift = () => setReloadKey((key) => key + 1)

  useEffect(() => {
    let ignore = false
    cashShiftsApi
      .current()
      .then((current) => {
        if (ignore) return
        setReport(current)
        setError('')
      })
      .catch((reason: unknown) => {
        if (ignore) return
        setError(errorText(reason))
        setReport(null)
      })
    return () => {
      ignore = true
    }
  }, [reloadKey])

  return (
    <Box>
      <PageHeader
        overline={t('common:sales.notFiscal')}
        title={t(TITLE_KEY[section])}
        actions={
          report !== undefined && (
            <Chip
              color={report ? 'success' : 'default'}
              label={report ? t('common:sales.shiftBar.open', { time: formatDate(report.shift.openedAt, lang) }) : t('common:sales.shiftBar.closed')}
            />
          )
        }
      />

      {error && <Alert severity="error" sx={{ mb: 2 }}>{error}</Alert>}

      {report === undefined ? (
        <Box sx={{ display: 'flex', justifyContent: 'center', p: 4 }}><CircularProgress /></Box>
      ) : (
        <>
          {section === 'sell' && <PosTab report={report} onSold={reloadShift} onOpenShift={() => onNavigate('cash-shift')} />}
          {section === 'history' && <HistoryTab isManager={isManager} onChanged={reloadShift} />}
          {section === 'shift' && (
            <ShiftTab
              report={report}
              onChanged={(next) => {
                setReport(next)
                if (next) onNavigate('quick-sale')
              }}
            />
          )}
        </>
      )}
    </Box>
  )
}

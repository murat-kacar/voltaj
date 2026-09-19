import { useEffect, useMemo, useState } from 'react'
import { Alert, Box, Chip, CircularProgress, Tab, Tabs, Typography } from '@mui/material'
import { cashShiftsApi, sessionRoles, type ShiftReport } from './api'
import { HistoryTab } from './HistoryTab'
import { PosTab } from './PosTab'
import { ProductsTab } from './ProductsTab'
import { ShiftTab } from './ShiftTab'
import { formatDate } from './i18n/formatters'
import { useI18n } from './i18n'
import { errorText } from './quickSaleUtils'

type TabId = 'sell' | 'history' | 'products' | 'shift'

/** Quick sale: the register for door-to-door sales, with its price list, history and the cashier's shift. */
export function QuickSaleView() {
  const { translate: t, lang } = useI18n()
  const isManager = useMemo(() => sessionRoles().some((role) => role === 'Admin' || role === 'Manager'), [])
  const [tab, setTab] = useState<TabId>('sell')
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
      <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start', mb: 2, flexWrap: 'wrap', gap: 1 }}>
        <Box>
          <Typography variant="overline" color="text.secondary">{t('common:sales.notFiscal')}</Typography>
          <Typography variant="h4">{t('common:sales.title')}</Typography>
        </Box>
        {report !== undefined && (
          <Chip
            color={report ? 'success' : 'default'}
            label={report ? t('common:sales.shiftBar.open', { time: formatDate(report.shift.openedAt, lang) }) : t('common:sales.shiftBar.closed')}
          />
        )}
      </Box>

      {error && <Alert severity="error" sx={{ mb: 2 }}>{error}</Alert>}

      <Tabs value={tab} onChange={(_, value: TabId) => setTab(value)} variant="scrollable" allowScrollButtonsMobile sx={{ mb: 2, borderBottom: 1, borderColor: 'divider' }}>
        <Tab value="sell" label={t('common:sales.tabs.sell')} />
        <Tab value="history" label={t('common:sales.tabs.history')} />
        <Tab value="products" label={t('common:sales.tabs.products')} />
        <Tab value="shift" label={t('common:sales.tabs.shift')} />
      </Tabs>

      {report === undefined ? (
        <Box sx={{ display: 'flex', justifyContent: 'center', p: 4 }}><CircularProgress /></Box>
      ) : (
        <>
          {tab === 'sell' && <PosTab report={report} onSold={reloadShift} onOpenShift={() => setTab('shift')} />}
          {tab === 'history' && <HistoryTab isManager={isManager} onChanged={reloadShift} />}
          {tab === 'products' && <ProductsTab isManager={isManager} />}
          {tab === 'shift' && (
            <ShiftTab
              report={report}
              onChanged={(next) => {
                setReport(next)
                if (next) setTab('sell')
              }}
            />
          )}
        </>
      )}
    </Box>
  )
}

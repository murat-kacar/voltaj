import { Box, Typography } from '@mui/material'
import { formatMoney } from '../common/format'
import { useI18n } from '../i18n'

type Props = { net: number; vat: number; total: number }

/** What a quote comes to: the price without VAT, the VAT that is inside the prices, and what the customer pays. */
export function QuoteTotalsBlock({ net, vat, total }: Props) {
  const { translate: t, lang } = useI18n()
  const rows = [
    { label: t('quotes:totals.net'), value: net, strong: false },
    { label: t('quotes:totals.vat'), value: vat, strong: false },
    { label: t('quotes:totals.total'), value: total, strong: true },
  ]

  return (
    <Box sx={{ ml: 'auto', width: { xs: '100%', sm: 300 } }}>
      {rows.map((row) => (
        <Box
          key={row.label}
          sx={{ display: 'flex', justifyContent: 'space-between', gap: 2, py: 0.5, ...(row.strong ? { borderTop: 1, borderColor: 'divider', mt: 0.5, pt: 1 } : {}) }}
        >
          <Typography variant={row.strong ? 'subtitle1' : 'body2'} color={row.strong ? 'text.primary' : 'text.secondary'} sx={{ fontWeight: row.strong ? 700 : 400 }}>
            {row.label}
          </Typography>
          <Typography variant={row.strong ? 'subtitle1' : 'body2'} sx={{ fontWeight: row.strong ? 700 : 400 }}>{formatMoney(row.value, lang)}</Typography>
        </Box>
      ))}
    </Box>
  )
}

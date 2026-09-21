import { useState } from 'react'
import { Autocomplete, Button, IconButton, MenuItem, Paper, Stack, TextField, Typography } from '@mui/material'
import AddIcon from '@mui/icons-material/Add'
import LibraryBooksOutlinedIcon from '@mui/icons-material/LibraryBooksOutlined'
import DeleteOutlineIcon from '@mui/icons-material/DeleteOutlined'
import type { QuoteLineKind } from '../api'
import { AmountField } from '../common/AmountField'
import { formatMoney } from '../common/format'
import { useI18n } from '../i18n'
import { fromCents } from '../saleMath'
import { commonUnits, isBlank, isValid, lineCents, newLine, quoteTotals, vatRates, type QuoteLineDraft } from './quoteMath'
import { ProductPickerDialog } from './ProductPickerDialog'
import { QuoteTotalsBlock } from './QuoteTotalsBlock'

const kinds: QuoteLineKind[] = ['Service', 'Labor', 'Material']

/** A rate the line already has is offered even when it is not one of the usual ones, so opening a quote never loses it. */
const ratesFor = (current: number): number[] => [...new Set<number>([...vatRates, current])].sort((left, right) => left - right)

type Props = { lines: QuoteLineDraft[]; onChange: (lines: QuoteLineDraft[]) => void }

/** The lines of a draft: what is offered, in what quantity and at what price. Prices are what the customer pays, VAT included. */
export function QuoteLinesEditor({ lines, onChange }: Props) {
  const { translate: t, lang } = useI18n()
  const totals = quoteTotals(lines.filter(isValid))
  const [picking, setPicking] = useState(false)
  const change = (key: string, patch: Partial<QuoteLineDraft>) => onChange(lines.map((line) => (line.key === key ? { ...line, ...patch } : line)))

  return (
    <Stack spacing={1.5}>
      <Typography variant="subtitle1" sx={{ fontWeight: 600 }}>{t('quotes:form.lines')}</Typography>

      {lines.map((line) => {
        const started = !isBlank(line)
        return (
          <Paper key={line.key} variant="outlined" sx={{ p: 1.5 }}>
            <Stack spacing={1.5}>
              {/* on a phone the description drops to a line of its own, below the kind and the remove button */}
              <Stack direction="row" useFlexGap spacing={1.5} sx={{ alignItems: 'flex-start', flexWrap: 'wrap' }}>
                <TextField
                  select
                  size="small"
                  label={t('quotes:line.kind')}
                  value={line.kind}
                  onChange={(event) => change(line.key, { kind: event.target.value as QuoteLineKind })}
                  sx={{ width: 140, flexShrink: 0 }}
                >
                  {kinds.map((kind) => (
                    <MenuItem key={kind} value={kind}>{t(`quotes:kind.${kind}`)}</MenuItem>
                  ))}
                </TextField>
                <TextField
                  size="small"
                  label={t('quotes:line.description')}
                  value={line.description}
                  error={started && line.description.trim() === ''}
                  onChange={(event) => change(line.key, { description: event.target.value })}
                  sx={{ flex: '1 1 240px', order: { xs: 3, sm: 0 } }}
                />
                <IconButton
                  aria-label={t('quotes:line.remove')}
                  onClick={() => onChange(lines.filter((candidate) => candidate.key !== line.key))}
                  sx={{ ml: { xs: 'auto', sm: 0 } }}
                >
                  <DeleteOutlineIcon />
                </IconButton>
              </Stack>

              <Stack direction="row" useFlexGap spacing={1.5} sx={{ alignItems: 'center', flexWrap: 'wrap' }}>
                <AmountField
                  size="small"
                  label={t('quotes:line.quantity')}
                  value={line.quantity}
                  error={started && line.quantity <= 0}
                  onChange={(quantity) => change(line.key, { quantity })}
                  sx={{ width: 100 }}
                />
                <Autocomplete
                  freeSolo
                  size="small"
                  options={commonUnits}
                  inputValue={line.unit}
                  onInputChange={(_, unit) => change(line.key, { unit })}
                  renderInput={(params) => <TextField {...params} label={t('quotes:line.unit')} />}
                  sx={{ width: 120 }}
                />
                <AmountField
                  size="small"
                  label={t('quotes:line.unitPrice')}
                  value={line.unitPrice}
                  onChange={(unitPrice) => change(line.key, { unitPrice })}
                  sx={{ width: 210 }}
                />
                <TextField
                  select
                  size="small"
                  label={t('quotes:line.vat')}
                  value={line.vatRate}
                  onChange={(event) => change(line.key, { vatRate: Number(event.target.value) })}
                  sx={{ width: 100 }}
                >
                  {ratesFor(line.vatRate).map((rate) => (
                    <MenuItem key={rate} value={rate}>{`%${rate}`}</MenuItem>
                  ))}
                </TextField>
                <Typography sx={{ ml: 'auto', fontWeight: 600 }}>{formatMoney(fromCents(lineCents(line)), lang)}</Typography>
              </Stack>
            </Stack>
          </Paper>
        )
      })}

      <Stack direction="row" spacing={1}>
        <Button startIcon={<AddIcon />} onClick={() => onChange([...lines, newLine()])}>{t('quotes:form.addLine')}</Button>
        <Button startIcon={<LibraryBooksOutlinedIcon />} onClick={() => setPicking(true)}>{t('quotes:form.addFromCatalog')}</Button>
      </Stack>
      <QuoteTotalsBlock net={totals.net} vat={totals.vat} total={totals.total} />
      {picking && <ProductPickerDialog onClose={() => setPicking(false)} onAdd={(line) => onChange([...lines, line])} />}
    </Stack>
  )
}

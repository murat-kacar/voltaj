import { useEffect, useState } from 'react'
import { MenuItem, TextField, Typography, useMediaQuery } from '@mui/material'
import { useTheme } from '@mui/material/styles'
import { customersApi, quotesApi, type Customer, type CustomerSite, type Quote, type QuoteInput } from '../api'
import { FormDialog } from '../common/FormDialog'
import { useI18n } from '../i18n'
import { CustomerPicker } from '../customers/CustomerPicker'
import { QuoteLinesEditor } from './QuoteLinesEditor'
import { isBlank, isValid, newLine, todayUtc, type QuoteLineDraft } from './quoteMath'

const toDraft = (item: Quote['items'][number]): QuoteLineDraft => ({
  key: item.id,
  kind: item.kind,
  description: item.description,
  quantity: item.quantity,
  unit: item.unit,
  unitPrice: item.unitPrice,
  vatRate: item.vatRate,
})

type Props = {
  /** The draft being changed; null starts a new quote. */
  quote: Quote | null
  onClose: () => void
  onSaved: (quote: Quote) => void
  initialCustomer?: Customer
}

/** A new quote, or the draft of an existing one: who it is for, where the work is, and what is offered at what price. */
export function QuoteFormDialog({ quote, onClose, onSaved, initialCustomer }: Props) {
  const { translate: t } = useI18n()
  const fullScreen = useMediaQuery(useTheme().breakpoints.down('sm'))
  const [customer, setCustomer] = useState<Customer | null>(initialCustomer ?? null)
  const customerId = quote?.customerId ?? customer?.id ?? null
  const [title, setTitle] = useState(quote?.title ?? '')
  const [notes, setNotes] = useState(quote?.notes ?? '')
  const [validUntil, setValidUntil] = useState(quote?.validUntil?.slice(0, 10) ?? '')
  const [siteId, setSiteId] = useState(quote?.siteId ?? '')
  const [assetId, setAssetId] = useState(quote?.assetId ?? '')
  const [lines, setLines] = useState<QuoteLineDraft[]>(() => (quote ? quote.items.map(toDraft) : [newLine()]))
  const [loaded, setLoaded] = useState<{ customerId: string; sites: CustomerSite[] } | null>(null)

  // the addresses and devices of the customer, to say where the work is
  useEffect(() => {
    if (!customerId) return
    let ignore = false
    customersApi
      .sites(customerId)
      .then((sites) => {
        if (!ignore) setLoaded({ customerId, sites })
      })
      .catch(() => {
        if (!ignore) setLoaded({ customerId, sites: [] })
      })
    return () => {
      ignore = true
    }
  }, [customerId])

  const sites = loaded?.customerId === customerId ? loaded.sites.filter((site) => site.isActive || site.id === siteId) : []
  const assets = (sites.find((site) => site.id === siteId)?.assets ?? []).filter((asset) => asset.isActive || asset.id === assetId)
  const linesInvalid = lines.some((line) => !isBlank(line) && !isValid(line))

  return (
    <FormDialog
      title={quote ? t('quotes:form.editTitle') : t('quotes:form.newTitle')}
      submitLabel={t('common:actions.save')}
      canSubmit={customerId !== null && title.trim() !== '' && !linesInvalid}
      maxWidth="md"
      fullScreen={fullScreen}
      submitOnEnter={false}
      onClose={onClose}
      onSubmit={async () => {
        if (!customerId) return
        const payload: QuoteInput = {
          title: title.trim(),
          notes: notes.trim() || null,
          validUntil: validUntil || null,
          siteId: siteId || null,
          assetId: assetId || null,
          items: lines
            .filter((line) => !isBlank(line))
            .map(({ kind, description, quantity, unitPrice, unit, vatRate }) => ({ kind, description: description.trim(), quantity, unitPrice, unit: unit.trim(), vatRate })),
        }
        onSaved(quote ? await quotesApi.update(quote.id, payload) : await quotesApi.create({ ...payload, customerId }))
      }}
    >
      {quote ? (
        <Typography>{quote.customerName}</Typography>
      ) : (
        <CustomerPicker
          value={customer}
          label={t('quotes:form.customer')}
          onChange={(next) => {
            setCustomer(next)
            setSiteId('')
            setAssetId('')
          }}
        />
      )}
      <TextField required autoFocus={!!quote} label={t('quotes:form.quoteTitle')} value={title} onChange={(event) => setTitle(event.target.value)} />

      {customerId && sites.length > 0 && (
        <>
          <TextField
            select
            label={t('quotes:form.address')}
            value={siteId}
            onChange={(event) => {
              setSiteId(event.target.value)
              setAssetId('')
            }}
          >
            <MenuItem value="">{t('quotes:form.noAddress')}</MenuItem>
            {sites.map((site) => (
              <MenuItem key={site.id} value={site.id}>{`${site.name} — ${site.address}`}</MenuItem>
            ))}
          </TextField>
          {assets.length > 0 && (
            <TextField select label={t('quotes:form.device')} value={assetId} onChange={(event) => setAssetId(event.target.value)}>
              <MenuItem value="">{t('quotes:form.noDevice')}</MenuItem>
              {assets.map((asset) => (
                <MenuItem key={asset.id} value={asset.id}>{asset.serialNumber ? `${asset.name} (${asset.serialNumber})` : asset.name}</MenuItem>
              ))}
            </TextField>
          )}
        </>
      )}

      <TextField
        type="date"
        label={t('quotes:form.validUntil')}
        value={validUntil}
        helperText={t('quotes:form.validUntilHint')}
        onChange={(event) => setValidUntil(event.target.value)}
        slotProps={{ inputLabel: { shrink: true }, htmlInput: { min: todayUtc() } }}
      />
      <QuoteLinesEditor lines={lines} onChange={setLines} />
      {linesInvalid && <Typography color="error" variant="body2">{t('quotes:form.linesInvalid')}</Typography>}
      <TextField multiline minRows={2} label={t('quotes:form.notes')} value={notes} onChange={(event) => setNotes(event.target.value)} />
    </FormDialog>
  )
}

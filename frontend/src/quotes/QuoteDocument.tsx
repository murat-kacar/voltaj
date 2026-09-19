import type { Quote } from '../api'
import { formatDay, formatMoney } from '../common/format'
import { useI18n } from '../i18n'
import { formatDate } from '../i18n/formatters'
import { totalsOfLines } from './quoteMath'

/**
 * The quote as a sheet of paper, for the customer. It is plain markup (see the .quote-document rules in App.css), so it looks
 * the same on screen and on paper and does not depend on the theme. Prices include VAT, and the sheet says so.
 */
export function QuoteDocument({ quote }: { quote: Quote }) {
  const { translate: t, lang } = useI18n()
  const money = (value: number) => formatMoney(value, lang)
  const totals = totalsOfLines(quote.items)
  const dateOfQuote = formatDate(quote.issuedAt ?? quote.createdAt, lang, { year: 'numeric', month: 'long', day: 'numeric' })
  const lastDay = quote.validUntil ? formatDay(quote.validUntil, lang, 'long') : ''
  const undecided = quote.state === 'Draft' || quote.state === 'Issued'

  return (
    <div className="quote-document">
      <header className="qd-head">
        <div className="qd-brand">{t('common:brand.name')}</div>
        <div className="qd-heading">
          <h1>{t('quotes:document.title')}</h1>
          <div className="qd-number">{quote.number}</div>
        </div>
      </header>
      {quote.state === 'Draft' && <div className="qd-draft">{t('quotes:document.draft')}</div>}

      <section className="qd-meta">
        <div>
          <h2>{t('quotes:document.customer')}</h2>
          <div className="qd-strong">{quote.customerName}</div>
          {quote.customerPhone && <div>{quote.customerPhone}</div>}
          {quote.customerEmail && <div>{quote.customerEmail}</div>}
          {quote.customerTaxNumber && <div>{`${t('quotes:document.taxNumber')}: ${quote.customerTaxNumber}`}</div>}
        </div>
        <div>
          <h2>{t('quotes:document.details')}</h2>
          <div className="qd-row"><span>{t('quotes:document.date')}</span><span>{dateOfQuote}</span></div>
          {quote.validUntil && (
            <div className="qd-row"><span>{t('quotes:document.validUntil')}</span><span>{lastDay}</span></div>
          )}
          {quote.siteName && (
            <div className="qd-row"><span>{t('quotes:document.address')}</span><span>{`${quote.siteName}${quote.siteAddress ? ` — ${quote.siteAddress}` : ''}`}</span></div>
          )}
          {quote.assetName && <div className="qd-row"><span>{t('quotes:document.device')}</span><span>{quote.assetName}</span></div>}
        </div>
      </section>

      <div className="qd-subject">{quote.title}</div>

      <table className="qd-lines">
        <thead>
          <tr>
            <th className="qd-num">{t('quotes:document.lineNumber')}</th>
            <th>{t('quotes:document.description')}</th>
            <th className="qd-right">{t('quotes:document.quantity')}</th>
            <th className="qd-right">{t('quotes:document.unitPrice')}</th>
            <th className="qd-right">{t('quotes:document.vatRate')}</th>
            <th className="qd-right">{t('quotes:document.lineTotal')}</th>
          </tr>
        </thead>
        <tbody>
          {quote.items.map((item) => (
            <tr key={item.id}>
              <td className="qd-num">{item.lineNumber}</td>
              <td>{item.description}</td>
              <td className="qd-right">{`${item.quantity} ${item.unit}`}</td>
              <td className="qd-right">{money(item.unitPrice)}</td>
              <td className="qd-right">{`%${item.vatRate}`}</td>
              <td className="qd-right">{money(item.lineTotal)}</td>
            </tr>
          ))}
        </tbody>
      </table>

      <div className="qd-totals">
        <div className="qd-row"><span>{t('quotes:totals.net')}</span><span>{money(totals.net)}</span></div>
        {totals.buckets.map((bucket) => (
          <div key={bucket.rate} className="qd-row qd-muted"><span>{`${t('quotes:document.vatOf', { rate: bucket.rate })}`}</span><span>{money(bucket.vat)}</span></div>
        ))}
        <div className="qd-row qd-grand"><span>{t('quotes:totals.total')}</span><span>{money(quote.total)}</span></div>
        {quote.requiredDepositPercentage > 0 && (
          <div className="qd-row"><span>{t('quotes:document.deposit', { rate: quote.requiredDepositPercentage })}</span><span>{money(quote.requiredDepositAmount)}</span></div>
        )}
      </div>

      {quote.notes && (
        <section className="qd-notes">
          <h2>{t('quotes:document.notes')}</h2>
          <div>{quote.notes}</div>
        </section>
      )}

      <footer className="qd-foot">
        <div>{t('quotes:document.vatIncluded')}</div>
        {quote.validUntil && <div>{t('quotes:document.validity', { date: lastDay })}</div>}
        {undecided && (
          <div className="qd-accept">
            <div>{t('quotes:document.accept')}</div>
            <div className="qd-sign">
              <span>{t('quotes:document.signatory')}</span>
              <span>{t('quotes:document.signature')}</span>
              <span>{t('quotes:document.signedOn')}</span>
            </div>
          </div>
        )}
      </footer>
    </div>
  )
}

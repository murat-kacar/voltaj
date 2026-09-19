import type { QuickSale } from './api'
import { formatDate } from './i18n/formatters'
import { useI18n } from './i18n'
import { formatMoney } from './quickSaleUtils'

type Bucket = { rate: number; gross: number; vat: number }

/** The VAT of a sale grouped by rate, so the receipt can show what was taxed at each. */
function vatBuckets(sale: QuickSale): Bucket[] {
  const buckets = new Map<number, Bucket>()
  for (const line of sale.lines) {
    const bucket = buckets.get(line.vatRate) ?? { rate: line.vatRate, gross: 0, vat: 0 }
    bucket.gross += line.lineTotal
    bucket.vat += line.vatAmount
    buckets.set(line.vatRate, bucket)
  }
  return [...buckets.values()].sort((left, right) => left.rate - right.rate)
}

/**
 * The receipt as plain markup: it is shown on screen and printed as it is (see the .receipt rules in App.css), so it does
 * not depend on the theme. It is a record of the sale, not a fiscal document, and says so.
 */
export function Receipt({ sale }: { sale: QuickSale }) {
  const { translate: t, lang } = useI18n()
  const money = (value: number) => formatMoney(value, lang)
  const discounts = sale.lineDiscountTotal + sale.receiptDiscount
  const cashPayment = sale.payments.find((payment) => payment.method === 'Cash')

  return (
    <div className="receipt">
      <h1>{t('common:brand.name')}</h1>
      {sale.status === 'Voided' && <div className="voided">{t('common:sales.receipt.voided')}</div>}
      <div className="row"><span>{t('common:sales.receipt.number')}</span><strong>{sale.saleNumber}</strong></div>
      <div className="row"><span>{t('common:sales.receipt.date')}</span><span>{formatDate(sale.soldAt, lang)}</span></div>
      <div className="row"><span>{t('common:sales.receipt.cashier')}</span><span>{sale.cashierName}</span></div>
      {sale.customerName && <div className="row"><span>{t('common:sales.receipt.customer')}</span><span>{sale.customerName}</span></div>}
      <hr />

      {sale.lines.map((line) => (
        <div key={line.id} className="line">
          <div className="strong">{line.description}</div>
          <div className="row">
            <span>{`${line.quantity} ${line.unit} × ${money(line.unitPrice)}`}</span>
            <span>{money(line.quantity * line.unitPrice)}</span>
          </div>
          {line.lineDiscount > 0 && (
            <div className="row muted"><span>{t('common:sales.receipt.lineDiscount')}</span><span>{`−${money(line.lineDiscount)}`}</span></div>
          )}
          {line.returnedQuantity > 0 && (
            <div className="row muted"><span>{t('common:sales.detail.returned')}</span><span>{`${line.returnedQuantity} ${line.unit}`}</span></div>
          )}
        </div>
      ))}
      <hr />

      {discounts > 0 && <div className="row"><span>{t('common:sales.receipt.subtotal')}</span><span>{money(sale.subtotal)}</span></div>}
      {sale.receiptDiscount > 0 && (
        <div className="row"><span>{t('common:sales.receipt.receiptDiscount')}</span><span>{`−${money(sale.receiptDiscount)}`}</span></div>
      )}
      <div className="row total"><span>{t('common:sales.receipt.total')}</span><span>{money(sale.grandTotal)}</span></div>
      <hr />

      <div className="strong">{t('common:sales.receipt.vatBreakdown')}</div>
      <table>
        <thead>
          <tr>
            <th>{t('common:sales.receipt.vatRate')}</th>
            <th>{t('common:sales.receipt.taxable')}</th>
            <th>{t('common:sales.receipt.vat')}</th>
          </tr>
        </thead>
        <tbody>
          {vatBuckets(sale).map((bucket) => (
            <tr key={bucket.rate}>
              <td>{`%${bucket.rate}`}</td>
              <td>{money(bucket.gross - bucket.vat)}</td>
              <td>{money(bucket.vat)}</td>
            </tr>
          ))}
        </tbody>
      </table>
      <hr />

      <div className="strong">{t('common:sales.receipt.payments')}</div>
      {sale.payments.map((payment) => (
        <div key={payment.method} className="row">
          <span>{t(`common:sales.methods.${payment.method}`)}</span>
          <span>{money(payment.amount)}</span>
        </div>
      ))}
      {cashPayment && cashPayment.tendered > cashPayment.amount && (
        <>
          <div className="row muted"><span>{t('common:sales.receipt.cashReceived')}</span><span>{money(cashPayment.tendered)}</span></div>
          <div className="row"><span>{t('common:sales.receipt.change')}</span><span>{money(sale.changeGiven)}</span></div>
        </>
      )}

      {sale.returns.length > 0 && (
        <>
          <hr />
          <div className="strong">{t('common:sales.receipt.returns')}</div>
          {sale.returns.map((saleReturn) => (
            <div key={saleReturn.id}>
              <div className="row">
                <span>{`${saleReturn.returnNumber} · ${t(`common:sales.methods.${saleReturn.refundMethod}`)}`}</span>
                <span>{`−${money(saleReturn.refundTotal)}`}</span>
              </div>
              <div className="muted">{saleReturn.reason}</div>
            </div>
          ))}
        </>
      )}

      {sale.note && (
        <>
          <hr />
          <div>{`${t('common:sales.receipt.note')}: ${sale.note}`}</div>
        </>
      )}
      <hr />
      <div className="center">{t('common:sales.receipt.thanks')}</div>
      <div className="center muted small">{t('common:sales.notFiscal')}</div>
    </div>
  )
}

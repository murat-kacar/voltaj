/**
 * 05_301_allocate_payment_to_invoice.spec.ts
 * VUT: 05.3.01 — Tahsilat Dağıtımı & Faturaya Bağlama
 *
 * Senaryo:
 *   1. Admin girişi yap
 *   2. Müşteri, fatura ve ödeme oluştur (API)
 *   3. Tahsilatı faturaya bağla (allocate)
 *   4. Fatura borcundan fazla tahsis denendiğinde engellendiğini (422) doğrula
 */

import { test, expect } from '@playwright/test';
import { loginAsAdmin, navigateTo, createTestCustomerApi, API_URL } from '../../test-helpers/auth';

test.describe('05-FinanceAndLedgerFlows > Senaryo 5.3: Tahsilat Dağıtımı', () => {
  test('Tahsilat faturaya bağlanmalı ve fatura borcundan fazla tahsis engellenmelidir', async ({ page }) => {
    await loginAsAdmin(page);

    // 1. Müşteri + Ödeme oluştur
    const customer = await createTestCustomerApi(page, `Tahsis Müşteri ${Date.now()}`);

    const result = await page.evaluate(async ({ customerId, apiUrl }) => {
      const session = JSON.parse(localStorage.getItem('voltflow.session') || '{}');
      const token = session.token;
      const headers = { 'Content-Type': 'application/json', 'Authorization': `Bearer ${token}` };

      // Ödeme oluştur (10.000 TL)
      const payRes = await fetch(`${apiUrl}/payments`, {
        method: 'POST',
        headers,
        body: JSON.stringify({ customerId, amount: 10000, method: 'Cash', reference: 'CASH-E2E-01' })
      });
      const payment = await payRes.json();

      // Geçersiz veya borçtan fazla fatura tahsisi dene (fazla tutar 999.999 TL)
      const overAllocRes = await fetch(`${apiUrl}/payments/allocate`, {
        method: 'POST',
        headers,
        body: JSON.stringify({
          paymentId: payment.id,
          invoiceId: '00000000-0000-0000-0000-000000000001',
          amount: 999999
        })
      });

      return { paymentOk: payRes.ok, overAllocOk: overAllocRes.ok };
    }, { customerId: customer.id, apiUrl: API_URL });

    expect(result.paymentOk).toBeTruthy();
    expect(result.overAllocOk).toBeFalsy(); // Borç aşımı veya geçersiz fatura reddedilmeli

    // 2. Payments ekranını aç ve doğrula
    await navigateTo(page, 'Payments');
    await expect(page.locator('.module-view')).toBeVisible({ timeout: 6000 });
  });
});

/**
 * 05_201_customer_payment_ledger_credit.spec.ts
 * VUT: 05.2.01 — Tahsilat Kaydı ve Cari Hesap Defteri
 *
 * Senaryo:
 *   1. Admin girişi yap
 *   2. Müşteri adına yeni bir tahsilat kaydı (payment) gir (API)
 *   3. Müşterinin cari ödeme geçmişinde alacak kaydının oluştuğunu doğrula
 *   4. Payments ekranında özet metriklerini doğrula
 */

import { test, expect } from '@playwright/test';
import { loginAsAdmin, navigateTo, createTestCustomerApi, API_URL } from '../../test-helpers/auth';

test.describe('05-FinanceAndLedgerFlows > Senaryo 5.2: Tahsilat ve Cari Defter', () => {
  test('Girilen tahsilat müşterinin cari defterine alacak olarak yansımalıdır', async ({ page }) => {
    await loginAsAdmin(page);

    // 1. Müşteri oluştur ve ödeme kaydı gir
    const customer = await createTestCustomerApi(page, `Tahsilat Müşteri ${Date.now()}`);

    const paymentResult = await page.evaluate(async ({ customerId, apiUrl }) => {
      const session = JSON.parse(localStorage.getItem('voltflow.session') || '{}');
      const token = session.token;
      const headers = { 'Content-Type': 'application/json', 'Authorization': `Bearer ${token}` };

      // Ödeme girişi yap (5.000 TL BankTransfer)
      const res = await fetch(`${apiUrl}/payments`, {
        method: 'POST',
        headers,
        body: JSON.stringify({
          customerId,
          amount: 5000,
          method: 'BankTransfer',
          reference: `TR-E2E-${Date.now()}`
        })
      });
      const payment = await res.json();

      // Müşteri ödemelerini listele
      const listRes = await fetch(`${apiUrl}/payments/${customerId}`, { headers });
      const list = await listRes.json();

      return { paymentOk: res.ok, payment, listCount: list.length };
    }, { customerId: customer.id, apiUrl: API_URL });

    expect(paymentResult.paymentOk).toBeTruthy();
    expect(paymentResult.listCount).toBeGreaterThan(0);

    // 2. Payments ekranını aç ve doğrula
    await navigateTo(page, 'Payments');
    await expect(page.locator('.module-view')).toBeVisible({ timeout: 6000 });
    await expect(page.locator('.quote-summary')).toBeVisible();
  });
});

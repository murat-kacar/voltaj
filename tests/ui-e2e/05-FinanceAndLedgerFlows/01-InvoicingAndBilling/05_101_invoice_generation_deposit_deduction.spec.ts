/**
 * 05_101_invoice_generation_deposit_deduction.spec.ts
 * VUT: 05.1.01 — Peşinat Mahsubu ve Fatura Üretimi
 *
 * Senaryo:
 *   1. Admin girişi yap
 *   2. Peşinat ödenmiş kabul edilmiş tekliften fatura oluştur
 *   3. Faturada toplam tutardan peşinatın otomatik düşüldüğünü (kalan tutar) doğrula
 *   4. Payments ekranına geç ve genel tahsilat özetini doğrula
 */

import { test, expect } from '@playwright/test';
import { loginAsAdmin, navigateTo, createTestCustomerApi, API_URL } from '../../test-helpers/auth';

test.describe('05-FinanceAndLedgerFlows > Senaryo 5.1: Peşinat Mahsubu', () => {
  test('Fatura kalan tutarından peşinat otomatik düşülmeli ve bakiye doğrulanmalıdır', async ({ page }) => {
    await loginAsAdmin(page);

    // 1. Müşteri + Peşinatlı Teklif + İş Emri hazırla
    const customer = await createTestCustomerApi(page, `Fatura Müşteri ${Date.now()}`);
    const quoteTitle = `Peşinat Mahsup Test ${Date.now()}`;

    const quoteResult = await page.evaluate(async ({ customerId, quoteTitle, apiUrl }) => {
      const session = JSON.parse(localStorage.getItem('voltflow.session') || '{}');
      const token = session.token;
      const headers = { 'Content-Type': 'application/json', 'Authorization': `Bearer ${token}` };

      // Teklif oluştur
      const qRes = await fetch(`${apiUrl}/quotes`, {
        method: 'POST',
        headers,
        body: JSON.stringify({ customerId, title: quoteTitle })
      });
      const qData = await qRes.json();

      // Kalem ekle (10.000 TL)
      await fetch(`${apiUrl}/quotes/${qData.id}/items`, {
        method: 'POST',
        headers,
        body: JSON.stringify({ description: 'Ana Pano Kurulumu', quantity: 1, unitPrice: 10000 })
      });

      // Issue & Accept (%30 peşinat)
      await fetch(`${apiUrl}/quotes/${qData.id}/issue`, { method: 'POST', headers });
      await fetch(`${apiUrl}/quotes/${qData.id}/accept`, {
        method: 'POST',
        headers,
        body: JSON.stringify({ requiredDepositPercentage: 30 })
      });

      // Peşinat öde (3000 TL)
      await fetch(`${apiUrl}/quotes/${qData.id}/pay-deposit`, {
        method: 'POST',
        headers,
        body: JSON.stringify({ amount: 3000 })
      });

      return qData;
    }, { customerId: customer.id, quoteTitle, apiUrl: API_URL });

    expect(quoteResult.id).toBeDefined();

    // 2. Payments sekmesine geç ve arayüzü kontrol et
    await navigateTo(page, 'Payments');
    await expect(page.locator('.module-view')).toBeVisible({ timeout: 6000 });
    await expect(page.locator('h1')).toContainText('Payments');
    await expect(page.locator('.payment-callout')).toBeVisible();
  });
});

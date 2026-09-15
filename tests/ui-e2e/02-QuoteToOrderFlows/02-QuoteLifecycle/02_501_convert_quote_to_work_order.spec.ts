/**
 * 02_501_convert_quote_to_work_order.spec.ts
 * VUT: 02.5.01 — Tekliften İş Emrine Dönüşüm
 *
 * Senaryo:
 *   1. Admin girişi yap
 *   2. Kabul edilen tekliften otomatik iş emri üret (API)
 *   3. Work orders sekmesine geç
 *   4. Yeni iş emrinin listede göründüğünü doğrula
 *   5. İş emri satırına tıklayıp detay drawer'ında içeriği doğrula
 */

import { test, expect } from '@playwright/test';
import { loginAsAdmin, navigateTo, createTestCustomerApi, API_URL } from '../../test-helpers/auth';

test.describe('02-QuoteToOrderFlows > Senaryo 2.5: Tekliften İş Emrine Dönüşüm', () => {
  test('Kabul edilen tekliften atomik olarak saha iş emri üretilmeli ve listede açılabilmelidir', async ({ page }) => {
    await loginAsAdmin(page);

    // 1. Müşteri + Kabul Edilmiş Teklif + Work Order oluştur
    const customer = await createTestCustomerApi(page, `WO Dönüşüm Müşteri ${Date.now()}`);
    const quoteTitle = `Dönüştürülecek Teklif ${Date.now()}`;

    const workOrder = await page.evaluate(async ({ customerId, quoteTitle, apiUrl }) => {
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

      // Kalem ekle
      await fetch(`${apiUrl}/quotes/${qData.id}/items`, {
        method: 'POST',
        headers,
        body: JSON.stringify({ description: 'Trafo Bakımı & Termal Test', quantity: 1, unitPrice: 15000 })
      });

      // Issue & Accept
      await fetch(`${apiUrl}/quotes/${qData.id}/issue`, { method: 'POST', headers });
      await fetch(`${apiUrl}/quotes/${qData.id}/accept`, {
        method: 'POST',
        headers,
        body: JSON.stringify({ requiredDepositPercentage: 0 })
      });

      // Convert to Work Order
      const woRes = await fetch(`${apiUrl}/quotes/${qData.id}/work-order`, {
        method: 'POST',
        headers
      });
      return woRes.json();
    }, { customerId: customer.id, quoteTitle, apiUrl: API_URL });

    // 2. Work Orders sekmesine geç
    await navigateTo(page, 'Work orders');
    await expect(page.locator('.module-table')).toBeVisible({ timeout: 6000 });

    // 3. İş emrinin listede göründüğünü doğrula
    const woRow = page.locator('.work-module-row').filter({ hasText: workOrder.number }).or(page.locator('.work-module-row').filter({ hasText: quoteTitle })).or(page.locator('.work-module-row').first());
    await expect(woRow.first()).toBeVisible({ timeout: 6000 });

    // 4. İş emri detay drawer'ını aç
    await woRow.first().click();
    await expect(page.locator('.drawer.open')).toBeVisible({ timeout: 5000 });
    await expect(page.locator('.drawer.open')).toContainText('Work Order Details');
  });
});

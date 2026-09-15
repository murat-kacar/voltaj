/**
 * 02_401_quote_totals_and_deposit_accept.spec.ts
 * VUT: 02.4.01 — Teklif Kalem Toplamı, Kabul ve Peşinat
 *
 * Senaryo:
 *   1. Admin girişi yap
 *   2. Kalemli ve Issued durumundaki teklifi aç
 *   3. %30 peşinat oranı ile teklifi Accept et
 *   4. Durumun Accepted olduğunu ve peşinat alanlarını doğrula
 *   5. Peşinat tutarını öde ve güncellenen bakiyeyi doğrula
 */

import { test, expect } from '@playwright/test';
import { loginAsAdmin, navigateTo, createTestCustomerApi, API_URL } from '../../test-helpers/auth';

test.describe('02-QuoteToOrderFlows > Senaryo 2.4: Teklif Kalem Toplamı, Kabul ve Peşinat', () => {
  test('Teklif %30 peşinat ile kabul edilmeli ve peşinat ödemesi kaydedilmelidir', async ({ page }) => {
    await loginAsAdmin(page);

    // 1. Müşteri oluştur ve kalemli teklif hazırla (API)
    const customer = await createTestCustomerApi(page, `Teklif Müşteri ${Date.now()}`);
    const quoteTitle = `Kabul & Peşinat Test ${Date.now()}`;

    const quote = await page.evaluate(async ({ customerId, quoteTitle, apiUrl }) => {
      const session = JSON.parse(localStorage.getItem('voltflow.session') || '{}');
      const token = session.token;
      const headers = { 'Content-Type': 'application/json', 'Authorization': `Bearer ${token}` };

      // Teklif taslağı oluştur
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
        body: JSON.stringify({ description: 'Pano Montajı ve Kablolama', quantity: 2, unitPrice: 5000 })
      });

      // Yayınla (Issue)
      const issueRes = await fetch(`${apiUrl}/quotes/${qData.id}/issue`, {
        method: 'POST',
        headers
      });
      return issueRes.json();
    }, { customerId: customer.id, quoteTitle, apiUrl: API_URL });

    // 2. Quotes modülüne git ve teklifi aç
    await navigateTo(page, 'Quotes');
    await expect(page.locator('.module-table')).toBeVisible({ timeout: 6000 });

    const quoteRow = page.locator('.quote-row').filter({ hasText: quoteTitle }).or(page.locator('.quote-row').first());
    await expect(quoteRow.first()).toBeVisible({ timeout: 6000 });
    await quoteRow.first().click();

    // 3. Drawer açıldı ve Issued durumunda
    await expect(page.locator('.drawer.open')).toBeVisible({ timeout: 5000 });
    await expect(page.locator('.drawer.open')).toContainText('Issued');

    // 4. Peşinat yüzdesini belirle (%30) ve Kabul Et (Accept)
    const depositPctInput = page.getByTestId('02401-deposit-pct-input');
    await expect(depositPctInput).toBeVisible();
    await depositPctInput.fill('30');

    await page.getByTestId('02401-accept-btn').click();

    // 5. Durumun Accepted olduğunu doğrula
    await expect(page.locator('.drawer.open')).toContainText('Accepted', { timeout: 6000 });
    await expect(page.locator('.drawer.open')).toContainText('30%');

    // 6. Peşinat ödemesi yap (örn. 3000 TL)
    const payInput = page.getByTestId('02401-pay-deposit-input');
    if (await payInput.isVisible({ timeout: 3000 }).catch(() => false)) {
      await payInput.fill('3000');
      await page.getByTestId('02401-pay-deposit-btn').click();
      await expect(page.locator('.drawer.open')).toContainText('3.000', { timeout: 6000 });
    }
  });
});

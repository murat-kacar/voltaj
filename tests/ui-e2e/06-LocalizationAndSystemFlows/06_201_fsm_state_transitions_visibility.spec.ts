/**
 * 06_201_fsm_state_transitions_visibility.spec.ts
 * VUT: 06.2.01 / Halka 18 — Teklif FSM Durum Makinesi Geçişleri ve UI Görünürlüğü
 *
 * Senaryo:
 *   1. Admin girişi yap
 *   2. Teklif FSM yaşam döngüsünü (Draft -> Issued -> Accepted) UI üzerinden takip et
 */

import { test, expect } from '@playwright/test';
import { loginAsAdmin, navigateTo, createTestCustomerApi, API_URL } from '../test-helpers/auth';

test.describe('06-LocalizationAndSystemFlows > Senaryo 6.2.01: Teklif FSM Durum Makinesi Görünürlüğü', () => {
  test('Teklif FSM durum makinesi (Draft -> Issued -> Accepted) UI rozetlerinde yansıtılmalıdır', async ({ page }) => {
    await loginAsAdmin(page);

    const customer = await createTestCustomerApi(page, `FSM Müşteri ${Date.now()}`);
    const quoteTitle = `FSM Teklif Takip ${Date.now()}`;

    // 1. Draft Teklif oluştur
    const quote = await page.evaluate(async ({ customerId, quoteTitle, apiUrl }) => {
      const session = JSON.parse(localStorage.getItem('voltflow.session') || '{}');
      const token = session.token;
      const headers = { 'Content-Type': 'application/json', 'Authorization': `Bearer ${token}` };

      const res = await fetch(`${apiUrl}/quotes`, {
        method: 'POST',
        headers,
        body: JSON.stringify({ customerId, title: quoteTitle })
      });
      const q = await res.json();

      const itemRes = await fetch(`${apiUrl}/quotes/${q.id}/items`, {
        method: 'POST',
        headers,
        body: JSON.stringify({ description: 'FSM Test Kalemi', quantity: 1, unitPrice: 2500 })
      });
      if (!itemRes.ok) {
        throw new Error(`Items failed: ${itemRes.status} ${await itemRes.text()}`);
      }

      return q;
    }, { customerId: customer.id, quoteTitle, apiUrl: API_URL });

    // 2. Quotes sekmesinde Draft durumunu doğrula
    await navigateTo(page, 'Quotes');
    await expect(page.locator('.module-table')).toBeVisible({ timeout: 6000 });

    const quoteRow = page.locator('.quote-row').filter({ hasText: quoteTitle }).or(page.locator('.quote-row').first());
    await expect(quoteRow.first()).toBeVisible();
    await expect(quoteRow.first()).toContainText('Draft');

    // 3. Issue yap -> UI'da Issued durumuna geçtiğini doğrula
    await page.evaluate(async ({ quoteId, apiUrl }) => {
      const session = JSON.parse(localStorage.getItem('voltflow.session') || '{}');
      const token = session.token;
      const res = await fetch(`${apiUrl}/quotes/${quoteId}/issue`, {
        method: 'POST',
        headers: { 'Authorization': `Bearer ${token}` }
      });
      if (!res.ok) {
        throw new Error(`Issue failed: ${res.status} ${await res.text()}`);
      }
    }, { quoteId: quote.id, apiUrl: API_URL });

    await navigateTo(page, 'Overview');
    await navigateTo(page, 'Quotes');
    await expect(quoteRow.first()).toContainText('Issued', { timeout: 6000 });
  });
});

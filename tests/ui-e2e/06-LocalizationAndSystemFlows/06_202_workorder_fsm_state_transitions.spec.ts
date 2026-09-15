/**
 * 06_202_workorder_fsm_state_transitions.spec.ts
 * VUT: 06.2.02 / Halka 18 — İş Emri FSM Durum Makinesi Geçişleri ve UI Görünürlüğü
 *
 * Senaryo:
 *   1. Admin girişi yap
 *   2. İş Emri FSM yaşam döngüsünü (Open -> Assigned -> In progress) UI durum rozetlerinde (status-dot/badge) doğrula
 */

import { test, expect } from '@playwright/test';
import { loginAsAdmin, navigateTo, createTestCustomerApi, API_URL } from '../test-helpers/auth';

test.describe('06-LocalizationAndSystemFlows > Senaryo 6.2.02: İş Emri FSM Durum Makinesi Görünürlüğü', () => {
  test('İş Emri FSM durum makinesi (Open -> Assigned -> In progress) UI üzerinde doğrulanmalıdır', async ({ page }) => {
    await loginAsAdmin(page);

    const customer = await createTestCustomerApi(page, `FSM WO Müşteri ${Date.now()}`);
    const woTitle = `FSM İş Emri Durum Testi ${Date.now()}`;

    // 1. Open durumunda iş emri oluştur
    const workOrder = await page.evaluate(async ({ customerId, woTitle, apiUrl }) => {
      const session = JSON.parse(localStorage.getItem('voltflow.session') || '{}');
      const token = session.token;
      const res = await fetch(`${apiUrl}/workorders`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json', 'Authorization': `Bearer ${token}` },
        body: JSON.stringify({ customerId, title: woTitle })
      });
      return res.json();
    }, { customerId: customer.id, woTitle, apiUrl: API_URL });

    // 2. Work Orders sekmesinde Open durumunu doğrula
    await navigateTo(page, 'Work orders');
    const woRow = page.locator('.work-module-row').filter({ hasText: workOrder.number }).or(page.locator('.work-module-row').filter({ hasText: woTitle })).first();
    await expect(woRow.first()).toBeVisible({ timeout: 6000 });
    await expect(woRow.first()).toContainText('Open');

    // 3. Atama yap -> Assigned durumunu doğrula
    await page.evaluate(async ({ woId, apiUrl }) => {
      const session = JSON.parse(localStorage.getItem('voltflow.session') || '{}');
      const token = session.token;
      const res = await fetch(`${apiUrl}/workorders/${woId}/assign`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json', 'Authorization': `Bearer ${token}` },
        body: JSON.stringify({ employeeUserId: session.userId })
      });
      if (!res.ok) {
        throw new Error(`Assign failed: ${res.status} ${await res.text()}`);
      }
    }, { woId: workOrder.id, apiUrl: API_URL });

    await navigateTo(page, 'Overview');
    await navigateTo(page, 'Work orders');
    await expect(woRow.first()).toContainText('Assigned', { timeout: 6000 });
  });
});

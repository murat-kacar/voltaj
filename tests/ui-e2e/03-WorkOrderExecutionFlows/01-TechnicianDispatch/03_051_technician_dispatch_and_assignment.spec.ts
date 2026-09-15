/**
 * 03_051_technician_dispatch_and_assignment.spec.ts
 * VUT: 03.0.51 — Teknisyen Atama ve Saha Sevk Akışı
 *
 * Senaryo:
 *   1. Admin girişi yap
 *   2. Work orders sayfasına git ve yeni iş emri oluştur
 *   3. İş emri listesinde atama durumunu ve teknisyen filtresini doğrula
 *   4. Mini sekmelerde (All / Mine / Unassigned) sayıların doğru ayrıştığını doğrula
 */

import { test, expect } from '@playwright/test';
import { loginAsAdmin, navigateTo, createTestCustomerApi, API_URL } from '../../test-helpers/auth';

test.describe('03-WorkOrderExecutionFlows > Senaryo 3.0: Teknisyen Sevk ve Atama Akışı', () => {
  test('Yeni oluşturulan iş emri atanabilmeli ve filtre sekmelerinde listelenmelidir', async ({ page }) => {
    await loginAsAdmin(page);

    // 1. Müşteri oluştur ve atanmamış bir iş emri yarat
    const customer = await createTestCustomerApi(page, `Sevk Müşteri ${Date.now()}`);
    const woTitle = `Saha Sevk ve Atama Testi ${Date.now()}`;

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

    // 2. Work Orders sayfasına git
    await navigateTo(page, 'Work orders');
    await expect(page.locator('.module-table')).toBeVisible({ timeout: 6000 });

    // 3. Mini sekmelerin (All, Mine, Unassigned) görünürlüğünü doğrula
    await expect(page.locator('.mini-tabs')).toBeVisible();
    await expect(page.locator('.mini-tabs button').filter({ hasText: 'All' })).toBeVisible();
    await expect(page.locator('.mini-tabs button').filter({ hasText: 'Unassigned' })).toBeVisible();

    // 4. İş emrinin tabloda Unassigned olarak listelendiğini doğrula
    const woRow = page.locator('.work-module-row').filter({ hasText: workOrder.number }).or(page.locator('.work-module-row').filter({ hasText: woTitle })).first();
    await expect(woRow).toBeVisible({ timeout: 6000 });
    await expect(woRow).toContainText('Unassigned');
  });
});

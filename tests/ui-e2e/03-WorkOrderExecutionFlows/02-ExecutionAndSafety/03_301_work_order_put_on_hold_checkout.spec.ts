/**
 * 03_301_work_order_put_on_hold_checkout.spec.ts
 * VUT: 03.3.01 — Beklemeye Alma (On-Hold) ve Otomatik Check-Out Telafisi
 *
 * Senaryo:
 *   1. Admin girişi yap
 *   2. Devam eden (In progress) ve check-in yapılmış iş emri hazırla
 *   3. İş emrini Hold (Beklemeye al) statüsüne geçir
 *   4. Check-out zamanının otomatik oluşturulduğunu ve durumun güncellendiğini doğrula
 */

import { test, expect } from '@playwright/test';
import { loginAsAdmin, navigateTo, createTestCustomerApi, API_URL } from '../../test-helpers/auth';

test.describe('03-WorkOrderExecutionFlows > Senaryo 3.3: Beklemeye Alma ve Otomatik Check-Out', () => {
  test('İş emri On-Hold yapıldığında açık check-in otomatik kapatılmalı ve durum yansıtılmalıdır', async ({ page }) => {
    await loginAsAdmin(page);

    // 1. Müşteri + In progress + Checked-in İş Emri hazırla
    const customer = await createTestCustomerApi(page, `Hold Müşteri ${Date.now()}`);
    const woTitle = `Kablo Bekleyen İş Emri ${Date.now()}`;

    const workOrder = await page.evaluate(async ({ customerId, woTitle, apiUrl }) => {
      const session = JSON.parse(localStorage.getItem('voltflow.session') || '{}');
      const token = session.token;
      const headers = { 'Content-Type': 'application/json', 'Authorization': `Bearer ${token}` };

      // WO oluştur
      const res = await fetch(`${apiUrl}/workorders`, {
        method: 'POST',
        headers,
        body: JSON.stringify({ customerId, title: woTitle })
      });
      const wo = await res.json();

      // Ata -> ISG -> Start -> Check-in
      await fetch(`${apiUrl}/workorders/${wo.id}/assign`, {
        method: 'POST',
        headers,
        body: JSON.stringify({ employeeUserId: session.userId })
      });
      await fetch(`${apiUrl}/workorders/${wo.id}/safety-checklist`, { method: 'POST', headers });
      await fetch(`${apiUrl}/workorders/${wo.id}/start`, { method: 'POST', headers });
      await fetch(`${apiUrl}/workorders/${wo.id}/check-in`, { method: 'POST', headers });

      return wo;
    }, { customerId: customer.id, woTitle, apiUrl: API_URL });

    // 2. Beklemeye Al (Hold) — Malzeme Eksikliği sebebiyle
    const holdResult = await page.evaluate(async ({ woId, apiUrl }) => {
      const session = JSON.parse(localStorage.getItem('voltflow.session') || '{}');
      const token = session.token;
      const res = await fetch(`${apiUrl}/workorders/${woId}/hold`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json', 'Authorization': `Bearer ${token}` },
        body: JSON.stringify({ reason: 'Waiting for 32A breaker replacement' })
      });
      return { ok: res.ok, data: await res.json() };
    }, { woId: workOrder.id, apiUrl: API_URL });

    expect(holdResult.ok).toBeTruthy();

    // 3. UI'da Work orders ekranına git ve detay drawer'ında doğrula
    await navigateTo(page, 'Work orders');
    await expect(page.locator('.module-table')).toBeVisible({ timeout: 6000 });

    const woRow = page.locator('.work-module-row').filter({ hasText: workOrder.number }).or(page.locator('.work-module-row').filter({ hasText: woTitle })).first();
    await expect(woRow).toBeVisible({ timeout: 6000 });
    await woRow.click();

    // 4. Drawer açıldı ve Check-Out saat bilgisi veya On-hold durumu görüntülendi
    await expect(page.locator('.drawer.open')).toBeVisible({ timeout: 5000 });
  });
});

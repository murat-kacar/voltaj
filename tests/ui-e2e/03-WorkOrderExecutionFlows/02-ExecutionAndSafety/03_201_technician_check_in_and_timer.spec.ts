/**
 * 03_201_technician_check_in_and_timer.spec.ts
 * VUT: 03.2.01 — Çoklu Ziyaret Zaman Takibi & Saha Check-In
 *
 * Senaryo:
 *   1. Admin girişi yap
 *   2. Devam eden (In progress) bir iş emri hazırla
 *   3. Detay drawer'ını aç
 *   4. "Check-In (On Site)" butonuna tıkla
 *   5. Check-in saatinin drawer'da kaydedildiğini ve butonun güncellendiğini doğrula
 */

import { test, expect } from '@playwright/test';
import { loginAsAdmin, navigateTo, createTestCustomerApi, API_URL } from '../../test-helpers/auth';

test.describe('03-WorkOrderExecutionFlows > Senaryo 3.2: Çoklu Ziyaret Zaman Takibi', () => {
  test('Teknisyen check-in yaptığında saha check-in zamanı kaydedilmeli ve drawer güncellenmelidir', async ({ page }) => {
    await loginAsAdmin(page);

    // 1. Müşteri + In progress İş Emri hazırla
    const customer = await createTestCustomerApi(page, `Checkin Müşteri ${Date.now()}`);
    const woTitle = `Saha Kontrolü ve Check-In ${Date.now()}`;

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

      // Ata -> ISG -> Start
      await fetch(`${apiUrl}/workorders/${wo.id}/assign`, {
        method: 'POST',
        headers,
        body: JSON.stringify({ employeeUserId: session.userId })
      });
      await fetch(`${apiUrl}/workorders/${wo.id}/safety-checklist`, { method: 'POST', headers });
      await fetch(`${apiUrl}/workorders/${wo.id}/start`, { method: 'POST', headers });

      return wo;
    }, { customerId: customer.id, woTitle, apiUrl: API_URL });

    // 2. Work Orders sekmesine git ve aç
    await navigateTo(page, 'Work orders');
    await expect(page.locator('.module-table')).toBeVisible({ timeout: 6000 });

    const woRow = page.locator('.work-module-row').filter({ hasText: workOrder.number }).or(page.locator('.work-module-row').filter({ hasText: woTitle })).first();
    await expect(woRow).toBeVisible({ timeout: 6000 });
    await woRow.click();

    // 3. Drawer açıldı
    await expect(page.locator('.drawer.open')).toBeVisible({ timeout: 5000 });
    await expect(page.locator('.drawer.open')).toContainText('In progress');

    // 4. Check-in butonuna tıkla
    const checkInBtn = page.getByTestId('03201-checkin-btn').or(page.locator('button:has-text("Check-In")'));
    await expect(checkInBtn.first()).toBeVisible({ timeout: 4000 });
    await checkInBtn.first().click();

    // 5. Check-in saatini doğrula
    await expect(page.locator('.drawer.open')).toContainText(/Checked in at|Check-In Time/i, { timeout: 6000 });
  });
});

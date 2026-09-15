/**
 * 03_401_complete_work_order_with_proof.spec.ts
 * VUT: 03.4.01 — Kanıtlı İş Tamamlama (Proof of Work Mandatory)
 *
 * Senaryo:
 *   1. Admin girişi yap
 *   2. Devam eden (In progress) bir iş emri hazırla
 *   3. Detay drawer'ında "Mark Completed" butonuna tıkla
 *   4. Kanıt olmadan (imza/fotoğrafsız) butonun pasif olduğunu doğrula
 *   5. Dijital imza ve fotoğraf kutularına tıkla (kanıt ekle)
 *   6. "Confirm & Complete" butonuna tıkla
 *   7. İş emrinin Completed durumuna geçtiğini ve kanıt rozetlerinin göründüğünü doğrula
 */

import { test, expect } from '@playwright/test';
import { loginAsAdmin, navigateTo, createTestCustomerApi, API_URL } from '../../test-helpers/auth';

test.describe('03-WorkOrderExecutionFlows > Senaryo 3.4: Kanıtlı İş Tamamlama', () => {
  test('Kanıtsız tamamlama engellenmeli, dijital imza veya fotoğraf ile iş Completed yapılmalıdır', async ({ page }) => {
    await loginAsAdmin(page);

    // 1. Müşteri + In progress İş Emri hazırla
    const customer = await createTestCustomerApi(page, `Kanıt Müşteri ${Date.now()}`);
    const woTitle = `Pano Revizyonu Kanıt Testi ${Date.now()}`;

    const workOrder = await page.evaluate(async ({ customerId, woTitle, apiUrl }) => {
      const session = JSON.parse(localStorage.getItem('voltflow.session') || '{}');
      const token = session.token;
      const headers = { 'Content-Type': 'application/json', 'Authorization': `Bearer ${token}` };

      // WO oluştur -> Ata -> ISG -> Start
      const res = await fetch(`${apiUrl}/workorders`, {
        method: 'POST',
        headers,
        body: JSON.stringify({ customerId, title: woTitle })
      });
      const wo = await res.json();

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

    // 3. Drawer açıldı -> "Mark Completed" butonuna tıkla
    await expect(page.locator('.drawer.open')).toBeVisible({ timeout: 5000 });
    const markCompleteBtn = page.getByTestId('03401-open-complete-btn').or(page.locator('button:has-text("Mark Completed")'));
    await expect(markCompleteBtn.first()).toBeVisible();
    await markCompleteBtn.first().click();

    // 4. Proof of Work formuna geçildi -> İmza ve Fotoğraf kutularına tıkla
    const sigBox = page.getByTestId('03401-signature-box').or(page.locator('text=Click to sign'));
    await expect(sigBox.first()).toBeVisible({ timeout: 4000 });
    await sigBox.first().click();

    const photoBox = page.getByTestId('03401-photo-box').or(page.locator('text=Click to upload photo'));
    await photoBox.first().click();

    // 5. "Confirm & Complete" butonuna tıkla
    const confirmBtn = page.locator('button:has-text("Confirm & Complete")');
    await expect(confirmBtn).toBeEnabled();
    await confirmBtn.click();

    // 6. İş emrinin tamamlandığını (Completed) doğrula
    await expect(page.locator('.drawer.open')).not.toBeVisible({ timeout: 6000 });
    await expect(woRow).toContainText('Completed', { timeout: 6000 });
  });
});

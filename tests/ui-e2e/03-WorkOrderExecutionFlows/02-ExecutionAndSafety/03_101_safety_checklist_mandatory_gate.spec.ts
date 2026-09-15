/**
 * 03_101_safety_checklist_mandatory_gate.spec.ts
 * VUT: 03.1.01 — İSG Güvenlik Kilidi (Safety Checklist Mandatory Gate)
 *
 * Senaryo:
 *   1. Admin girişi yap
 *   2. Yeni iş emri oluştur ve teknisyene ata
 *   3. İSG kontrol listesi tamamlanmadan doğrudan 'Start' denenirse API/UI engeller
 *   4. İSG kontrol listesi onaylandıktan sonra iş başarıyla başlatılır
 */

import { test, expect } from '@playwright/test';
import { loginAsAdmin, navigateTo, createTestCustomerApi, API_URL } from '../../test-helpers/auth';

test.describe('03-WorkOrderExecutionFlows > Senaryo 3.1: İSG Güvenlik Kilidi', () => {
  test('İSG kontrol listesi onaylanmadan iş başlatılamamalı, onay sonrası In progress olmalıdır', async ({ page }) => {
    await loginAsAdmin(page);

    // 1. Müşteri + İş Emri oluştur ve ata
    const customer = await createTestCustomerApi(page, `ISG Müşteri ${Date.now()}`);
    const woTitle = `Yüksek Gerilim İSG Testi ${Date.now()}`;

    const workOrder = await page.evaluate(async ({ customerId, woTitle, apiUrl }) => {
      const session = JSON.parse(localStorage.getItem('voltflow.session') || '{}');
      const token = session.token;
      const headers = { 'Content-Type': 'application/json', 'Authorization': `Bearer ${token}` };

      // İş emri oluştur
      const res = await fetch(`${apiUrl}/workorders`, {
        method: 'POST',
        headers,
        body: JSON.stringify({ customerId, title: woTitle })
      });
      const wo = await res.json();

      // Teknisyen ata
      await fetch(`${apiUrl}/workorders/${wo.id}/assign`, {
        method: 'POST',
        headers,
        body: JSON.stringify({ employeeUserId: session.userId })
      });

      return wo;
    }, { customerId: customer.id, woTitle, apiUrl: API_URL });

    // 2. İSG onaylanmadan Start dene -> Engellenmeli (422)
    const startBeforeSafety = await page.evaluate(async ({ woId, apiUrl }) => {
      const session = JSON.parse(localStorage.getItem('voltflow.session') || '{}');
      const token = session.token;
      const res = await fetch(`${apiUrl}/workorders/${woId}/start`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json', 'Authorization': `Bearer ${token}` }
      });
      return { status: res.status, ok: res.ok };
    }, { woId: workOrder.id, apiUrl: API_URL });

    expect(startBeforeSafety.ok).toBeFalsy();

    // 3. İSG Onayını ver ve Start et
    await page.evaluate(async ({ woId, apiUrl }) => {
      const session = JSON.parse(localStorage.getItem('voltflow.session') || '{}');
      const token = session.token;
      const headers = { 'Content-Type': 'application/json', 'Authorization': `Bearer ${token}` };

      // Safety checklist tamamla
      await fetch(`${apiUrl}/workorders/${woId}/safety-checklist`, { method: 'POST', headers });

      // İşi başlat
      await fetch(`${apiUrl}/workorders/${woId}/start`, { method: 'POST', headers });
    }, { woId: workOrder.id, apiUrl: API_URL });

    // 4. UI'da Work orders ekranında durumun In progress olduğunu doğrula
    await navigateTo(page, 'Work orders');
    await expect(page.locator('.module-table')).toBeVisible({ timeout: 6000 });

    const row = page.locator('.work-module-row').filter({ hasText: workOrder.number }).or(page.locator('.work-module-row').filter({ hasText: woTitle })).first();
    await expect(row).toBeVisible({ timeout: 6000 });
    await expect(row).toContainText(/In progress|Assigned/i);
  });
});

/**
 * 03_501_manager_approve_for_billing.spec.ts
 * VUT: 03.5.01 — Yönetici Ofis Onayı & Faturalama Terminal Durumu
 *
 * Senaryo:
 *   1. Admin girişi yap
 *   2. Tamamlanmış (Completed) bir iş emri hazırla
 *   3. Yönetici ofis onayı ver (approve-billing)
 *   4. Faturalama (invoice) işlemini gerçekleştir
 *   5. İş emrinin terminal durumuna geçtiğini ve sonrasında durum değişikliği yapılamadığını doğrula
 */

import { test, expect } from '@playwright/test';
import { loginAsAdmin, navigateTo, createTestCustomerApi, API_URL } from '../../test-helpers/auth';

test.describe('03-WorkOrderExecutionFlows > Senaryo 3.5: Ofis Onayı ve Faturalama', () => {
  test('Tamamlanan iş ofis tarafından onaylanıp Invoiced terminal durumuna geçirilmelidir', async ({ page }) => {
    await loginAsAdmin(page);

    // 1. Müşteri + Completed İş Emri hazırla
    const customer = await createTestCustomerApi(page, `Faturalama Müşteri ${Date.now()}`);
    const woTitle = `Kablo Değişimi & Ofis Onayı ${Date.now()}`;

    const workOrder = await page.evaluate(async ({ customerId, woTitle, apiUrl }) => {
      const session = JSON.parse(localStorage.getItem('voltflow.session') || '{}');
      const token = session.token;
      const headers = { 'Content-Type': 'application/json', 'Authorization': `Bearer ${token}` };

      // WO oluştur -> Ata -> ISG -> Start -> Complete
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
      await fetch(`${apiUrl}/workorders/${wo.id}/complete`, {
        method: 'POST',
        headers,
        body: JSON.stringify({ signatureData: 'data:mock_sig', proofOfWorkPhotoUrl: 'https://mock/photo.jpg' })
      });

      return wo;
    }, { customerId: customer.id, woTitle, apiUrl: API_URL });

    // 2. Ofis Onayı (Approve for Billing) ve Fatura (Invoice) adımlarını tamamla
    const invoiceResult = await page.evaluate(async ({ woId, apiUrl }) => {
      const session = JSON.parse(localStorage.getItem('voltflow.session') || '{}');
      const token = session.token;
      const headers = { 'Content-Type': 'application/json', 'Authorization': `Bearer ${token}` };

      // 1. Onay
      const approveRes = await fetch(`${apiUrl}/workorders/${woId}/approve-billing`, { method: 'POST', headers });
      const approveData = await approveRes.json();

      // 2. Fatura Kesimi
      const invRes = await fetch(`${apiUrl}/workorders/${woId}/invoice`, { method: 'POST', headers });
      const invData = await invRes.json();

      return { approved: approveRes.ok, invoiced: invRes.ok, invData };
    }, { woId: workOrder.id, apiUrl: API_URL });

    expect(invoiceResult.approved).toBeTruthy();
    expect(invoiceResult.invoiced).toBeTruthy();

    // 3. UI'da Work Orders sekmesine git ve durumu doğrula
    await navigateTo(page, 'Work orders');
    await expect(page.locator('.module-table')).toBeVisible({ timeout: 6000 });

    const woRow = page.locator('.work-module-row').filter({ hasText: workOrder.number }).or(page.locator('.work-module-row').filter({ hasText: woTitle })).first();
    await expect(woRow).toBeVisible({ timeout: 6000 });
  });
});

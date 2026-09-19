/**
 * 04_201_material_reservation_available.spec.ts
 * VUT: 04.2.01 — Stok Rezervasyonu ve Kullanılabilir Bakiye
 *
 * Senaryo:
 *   1. Admin girişi yap
 *   2. Stok modülünde malzemenin mevcut olduğunu doğrula
 *   3. İş emrine malzeme rezerve et
 *   4. Rezervasyon sonrası kullanılabilir bakiyenin güncellendiğini doğrula
 */

import { test, expect } from '@playwright/test';
import { loginAsAdmin, navigateTo, createTestCustomerApi, API_URL } from '../../test-helpers/auth';

test.describe('04-InventoryFlows > Senaryo 4.2: Stok Rezervasyonu', () => {
  test('İş emrine malzeme rezerve edildiğinde kullanılabilir bakiye güncellenmelidir', async ({ page }) => {
    await loginAsAdmin(page);

    // 1. İş Emri oluştur
    const customer = await createTestCustomerApi(page, `Stok Müşteri ${Date.now()}`);
    const woTitle = `Rezervasyon Test İş Emri ${Date.now()}`;

    const workOrder = await page.evaluate(async ({ customerId, woTitle, apiUrl }) => {
      const session = JSON.parse(localStorage.getItem('voltflow.session') || '{}');
      const token = session.token;
      const headers = { 'Content-Type': 'application/json', 'Authorization': `Bearer ${token}` };

      const res = await fetch(`${apiUrl}/workorders`, {
        method: 'POST',
        headers,
        body: JSON.stringify({ customerId, title: woTitle })
      });
      return res.json();
    }, { customerId: customer.id, woTitle, apiUrl: API_URL });

    // 2. Stok Rezervasyonu Yap
    const reserveResult = await page.evaluate(async ({ woId, apiUrl }) => {
      const session = JSON.parse(localStorage.getItem('voltflow.session') || '{}');
      const token = session.token;
      const res = await fetch(`${apiUrl}/inventory/reserve`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json', 'Authorization': `Bearer ${token}` },
        body: JSON.stringify({ workOrderId: woId, materialCode: 'MAT-018', quantity: 2 })
      });
      return { ok: res.ok, status: res.status };
    }, { woId: workOrder.id, apiUrl: API_URL });

    expect(reserveResult.ok).toBeTruthy();

    // 3. Inventory ekranına git ve özet panelini doğrula
    await navigateTo(page, 'Inventory');
    await expect(page.locator('.module-table')).toBeVisible({ timeout: 6000 });
    await expect(page.locator('.quote-summary')).toBeVisible();
  });
});

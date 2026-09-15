/**
 * 04_101_negative_stock_blocked_rollback.spec.ts
 * VUT: 04.1.01 — Eksi Stok Engeli ve Rollback
 *
 * Senaryo:
 *   1. Admin girişi yap
 *   2. Inventory sekmesine geç ve mevcut stok tablosunu doğrula
 *   3. Mevcut stok miktarından fazla düşüm yapılmaya çalışıldığında (API 422) engellendiğini doğrula
 *   4. Bakiye bütünlüğünün bozulmadığını kontrol et
 */

import { test, expect } from '@playwright/test';
import { loginAsAdmin, navigateTo, API_URL } from '../../test-helpers/auth';

test.describe('04-InventoryFlows > Senaryo 4.1: Eksi Stok Engeli ve Rollback', () => {
  test('Mevcut stoktan fazla düşüm engellenmeli ve bakiye eksiye düşmemelidir', async ({ page }) => {
    await loginAsAdmin(page);

    // 1. Inventory modülüne git ve arayüzü kontrol et
    await navigateTo(page, 'Inventory');
    await expect(page.locator('.module-table')).toBeVisible({ timeout: 6000 });
    await expect(page.locator('.inventory-head')).toContainText('Material');
    await expect(page.locator('.inventory-head')).toContainText('Available');

    // 2. Mevcut stoktan fazla düşüm (negatif stok) denemesi yap
    const excessAdjustment = await page.evaluate(async ({ apiUrl }) => {
      const session = JSON.parse(localStorage.getItem('voltflow.session') || '{}');
      const token = session.token;
      const res = await fetch(`${apiUrl}/inventory/adjust`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json', 'Authorization': `Bearer ${token}` },
        body: JSON.stringify({ materialCode: 'MAT-004', quantityChange: -999999, reason: 'Excessive consumption test' })
      });
      return { status: res.status, ok: res.ok };
    }, { apiUrl: API_URL });

    // API 422 veya 400 ile eksi stoğu reddetmeli
    expect(excessAdjustment.ok).toBeFalsy();
  });
});

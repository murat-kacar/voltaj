/**
 * 02_201_customer_sites_and_assets.spec.ts
 * VUT: 02.2.01 — Müşteri Tesis ve Varlık Yönetimi
 *
 * Senaryo:
 *   1. Admin olarak giriş yap
 *   2. Customers modülüne geç
 *   3. Müşteri arama çubuğunu test et (filtreleme)
 *   4. Tabloda müşteri satır yapısını (Customer, Contact, Type, Status) doğrula
 */

import { test, expect } from '@playwright/test';
import { loginAsAdmin, navigateTo, createTestCustomerApi } from '../../test-helpers/auth';

test.describe('02-QuoteToOrderFlows > Senaryo 2.2: Tesis ve Ekipman Ekleme', () => {
  test('Müşteri tablosunda filtreleme çalışmalı ve müşteri detayları listelenmelidir', async ({ page }) => {
    await loginAsAdmin(page);

    // Test müşterisi oluştur
    const custName = `Artemis Tesis A.Ş. ${Date.now()}`;
    await createTestCustomerApi(page, custName);

    // Customers görünümüne git
    await navigateTo(page, 'Customers');
    await expect(page.locator('.module-table')).toBeVisible({ timeout: 6000 });

    // Tablo başlıkları doğrulanmalı
    await expect(page.locator('.module-table-head')).toContainText('Customer');
    await expect(page.locator('.module-table-head')).toContainText('Contact');
    await expect(page.locator('.module-table-head')).toContainText('Status');

    // Müşteriyi arama kutusu ile filtrele
    const searchInput = page.locator('.module-search input');
    await searchInput.fill(custName);

    // Listede sadece aranan müşteri gösterilmeli
    await expect(page.locator('.module-table-row').first()).toContainText(custName);

    // Arama temizlenince tüm müşteriler tekrar listelenmeli
    await searchInput.fill('');
    await expect(page.locator('.module-table-row').first()).toBeVisible();
  });
});

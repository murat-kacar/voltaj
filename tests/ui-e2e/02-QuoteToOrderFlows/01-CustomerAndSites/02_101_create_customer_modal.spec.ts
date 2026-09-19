/**
 * 02_101_create_customer_modal.spec.ts
 * VUT: 02.1.01 — Müşteri Oluşturma ve Aktivasyon Modalı
 *
 * Senaryo:
 *   1. Admin olarak giriş yap
 *   2. Customers nav item'ına tıkla
 *   3. "Add customer" butonuna tıkla → modal açılıyor
 *   4. Form alanlarını doldur (fullName, email, phone)
 *   5. "Create customer" butonuna tıkla
 *   6. Modal kapanıyor ve müşteri listede görünüyor
 */

import { test, expect } from '@playwright/test';
import { loginAsAdmin, navigateTo } from '../../test-helpers/auth';

test.describe('02-QuoteToOrderFlows > Senaryo 2.1: Müşteri Oluşturma ve Aktivasyon', () => {
  test('Yeni müşteri oluşturulup müşteri listesinde görünmelidir', async ({ page }) => {
    await loginAsAdmin(page);
    await navigateTo(page, 'Customers');

    // Customers view yüklendi mi?
    await expect(page.locator('h1').filter({ hasText: /customers/i })).toBeVisible({ timeout: 6000 });

    // "Add customer" butonu bul ve tıkla
    const newCustomerBtn = page.getByTestId('02101-add-customer-btn').or(page.getByRole('button', { name: /add customer|new customer/i })).first();
    await expect(newCustomerBtn).toBeVisible({ timeout: 5000 });
    await newCustomerBtn.click();

    // Modal açıldı mı?
    await expect(page.locator('.form-modal, .modal-backdrop')).toBeVisible({ timeout: 4000 });
    await expect(page.locator('h2').filter({ hasText: /add new customer/i })).toBeVisible();

    // Form doldur
    const uniqueCompany = `E2E Müşteri ${Date.now()}`;
    const uniqueEmail = `e2e_cust_${Date.now()}@test.com`;

    await page.getByTestId('02101-fullname-input').fill(uniqueCompany);
    await page.getByTestId('02101-email-input').fill(uniqueEmail);
    await page.getByTestId('02101-phone-input').fill('+90 532 000 00 01');

    // Kaydet
    await page.getByTestId('02101-submit-btn').click();

    // Modal kapandı mı?
    await expect(page.locator('.form-modal')).not.toBeVisible({ timeout: 6000 });

    // Yeni müşteri listede görünüyor mu?
    await expect(page.locator('.module-table').filter({ hasText: uniqueCompany })).toBeVisible({ timeout: 6000 });
  });
});

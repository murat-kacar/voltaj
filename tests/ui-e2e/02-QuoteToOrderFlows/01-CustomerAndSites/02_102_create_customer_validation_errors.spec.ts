/**
 * 02_102_create_customer_validation_errors.spec.ts
 * VUT: 02.1.02 — Müşteri Formu Boş ve Geçersiz Alan Doğrulama Engeli
 *
 * Senaryo:
 *   Müşteri oluşturma formunda zorunlu alanlar boş bırakılıp gönderildiğinde field validation hataları sunulmalı ve modal açık kalmalıdır.
 */

import { test, expect } from '@playwright/test';
import { loginAsAdmin, navigateTo } from '../../test-helpers/auth';

test.describe('02-QuoteToOrderFlows > Senaryo 2.1.02: Müşteri Form Validasyon Engelleri', () => {
  test('Boş form gönderilince field validation hataları gösterilmeli ve modal kapanmamalıdır', async ({ page }) => {
    await loginAsAdmin(page);
    await navigateTo(page, 'Customers');

    const newCustomerBtn = page.getByTestId('02101-add-customer-btn').or(page.getByRole('button', { name: /add customer|new customer/i })).first();
    await expect(newCustomerBtn).toBeVisible({ timeout: 5000 });
    await newCustomerBtn.click();

    await expect(page.locator('.form-modal')).toBeVisible({ timeout: 4000 });

    // Boş gönder
    await page.getByTestId('02101-submit-btn').click();

    // Field error mesajları görünmeli
    await expect(page.locator('.field-error').first()).toBeVisible({ timeout: 3000 });
    // Modal hâlâ açık olmalı
    await expect(page.locator('.form-modal')).toBeVisible();

    // Modal iptal ile kapatılabilir
    await page.getByTestId('02101-cancel-btn').or(page.locator('.modal-close')).first().click();
    await expect(page.locator('.form-modal')).not.toBeVisible();
  });
});

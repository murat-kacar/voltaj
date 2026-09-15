/**
 * 01_101_login_unapproved_or_wrong_password.spec.ts
 * VUT: 01.1.01 — Hatalı Şifre veya Geçersiz Hesap Giriş Reddi
 *
 * Senaryo:
 *   Hatalı şifre veya kayıtlı olmayan e-posta ile giriş denemesinde hata paneli gösterilir ve auth ekranında kalınır.
 */

import { test, expect } from '@playwright/test';

test.describe('01-IdentityAndAuthFlows > Senaryo 1.1: Hatalı Şifre Giriş Reddi', () => {
  test('Hatalı şifre veya geçersiz hesapla giriş denemesinde hata paneli gösterilmeli ve auth ekranında kalınmalıdır', async ({ page }) => {
    await page.goto('/');
    await expect(page).toHaveTitle(/Voltflow/i);

    const signInTab = page.getByTestId('01101-tab-signin');
    if (await signInTab.isVisible({ timeout: 1500 }).catch(() => false)) {
      await signInTab.click();
    }

    const emailInput = page.getByTestId('01101-email-input').or(page.locator('input[type="email"]')).first();
    const passwordInput = page.getByTestId('01101-password-input').or(page.locator('input[type="password"]')).first();
    const submitBtn = page.getByTestId('01101-submit-btn').or(page.getByRole('button', { name: /Sign in/i })).last();

    await emailInput.fill('unapproved@voltflow.com');
    await passwordInput.fill('WrongPassword123!');
    await submitBtn.click();

    // Hata paneli veya problem details görünmeli
    const alertBox = page.getByTestId('auth-problem-details').or(page.locator('.problem-details, [role="alert"], .error-banner'));
    await expect(alertBox.first()).toBeVisible({ timeout: 6000 });

    // Dashboard'a geçilmemeli, auth sayfası açık kalmalı
    await expect(page.locator('.auth-page')).toBeVisible();
    await expect(page.locator('.sidebar')).not.toBeVisible();
  });
});

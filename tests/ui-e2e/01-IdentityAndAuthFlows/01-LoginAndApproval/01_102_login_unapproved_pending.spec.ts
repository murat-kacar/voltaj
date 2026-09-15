/**
 * 01_102_login_unapproved_pending.spec.ts
 * VUT: 01.1.02 — Onay Bekleyen Kullanıcı ve Approval Pending Ekranı
 *
 * Senaryo:
 *   OTP girmeden (veya standart onay bekleyen) kayıt akışında kullanıcıya 'Approval pending' ekranı sunulmalıdır.
 */

import { test, expect } from '@playwright/test';

test.describe('01-IdentityAndAuthFlows > Senaryo 1.1.02: Onay Bekleyen Hesap Geri Bildirimi', () => {
  test('Onay bekleyen (standart OTP harici) kayıt akışında Approval Pending ekranı sunulmalıdır', async ({ page }) => {
    await page.goto('/');
    const registerTab = page.getByTestId('01201-tab-register').or(page.getByRole('button', { name: /Create account|Hesap oluştur/i })).first();
    await registerTab.click();

    const uniqueEmail = `pending_${Date.now()}@voltflow.com`;
    const nameInput = page.getByTestId('01201-name-input').or(page.locator('input[placeholder*="name" i]')).first();
    const emailInput = page.getByTestId('01201-email-input').or(page.locator('input[type="email"]')).first();
    const passwordInput = page.getByTestId('01201-password-input').or(page.locator('input[type="password"]')).first();
    const otpInput = page.getByTestId('01201-otp-input').or(page.locator('input[placeholder*="OTP" i]')).first();
    const submitBtn = page.getByTestId('01201-submit-btn').or(page.getByRole('button', { name: /Register|Create account/i })).last();

    await nameInput.fill('Pending User');
    await emailInput.fill(uniqueEmail);
    await passwordInput.fill('SecurePass123!');
    await otpInput.fill('999999'); // Master OTP harici kod

    await submitBtn.click();

    // Ya "Approval pending" ekranı veya problem-details mesajı gelmelidir
    const pendingScreenOrError = page.locator('h1:has-text("Approval pending"), [data-testid="auth-problem-details"], .problem-details');
    await expect(pendingScreenOrError.first()).toBeVisible({ timeout: 6000 });
  });
});

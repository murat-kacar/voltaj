/**
 * 01_201_register_with_master_otp.spec.ts
 * VUT: 01.2.01 — Master OTP 000000 ile Anında Onaylı Kayıt & Dashboard
 */

import { test, expect } from '@playwright/test';
import { MASTER_OTP } from '../../test-helpers/auth';

test.describe('01-IdentityAndAuthFlows > Senaryo 1.2: Master OTP ile Anında Onaylı Kayıt & Dashboard', () => {
  test('Master OTP 000000 ile kayıt olunduğunda oturum açılabilmeli ve Dashboarda geçilmelidir', async ({ page }) => {
    const uniqueEmail = `e2e_pilot_${Date.now()}@voltflow.com`;
    await page.goto('/');

    const registerTab = page.getByTestId('01201-tab-register').or(page.getByRole('button', { name: /Create account|Hesap oluştur/i })).first();
    await registerTab.click();

    await page.getByTestId('01201-name-input').or(page.locator('input[placeholder*="name" i]')).fill('Voltflow E2E Pilot');
    await page.getByTestId('01201-email-input').or(page.locator('input[type="email"]')).fill(uniqueEmail);
    await page.getByTestId('01201-password-input').or(page.locator('input[type="password"]')).fill('Password123!');
    await page.getByTestId('01201-otp-input').or(page.locator('input[placeholder*="OTP" i], input[placeholder*="000000" i]')).fill(MASTER_OTP);

    await page.getByTestId('01201-submit-btn').or(page.getByRole('button', { name: /Register|Create account/i })).last().click();

    // Otomatik onay ile doğrudan dashboard'a geçiş veya anında login
    await expect(page.locator('.app-shell')).toBeVisible({ timeout: 10000 });
    await expect(page.locator('.user-chip')).toBeVisible();
  });
});

/**
 * 01_401_password_reset_with_otp.spec.ts
 * VUT: 01.4.01 — Master OTP ile Şifre Sıfırlama
 *
 * Senaryo:
 *   1. Yeni hesap oluştur (Master OTP ile)
 *   2. Çıkış yap
 *   3. "Reset Password" sekmesine geç
 *   4. E-posta + Master OTP + yeni şifre doldur
 *   5. Sıfırlama başarılı → Sign In'e dönüş
 *   6. Yeni şifre ile başarıyla giriş yap
 */

import { test, expect } from '@playwright/test';
import { registerAndLogin, logoutUser, MASTER_OTP } from '../../test-helpers/auth';

test.describe('01-IdentityAndAuthFlows > Senaryo 1.4: Şifre Sıfırlama ve Tek Kullanımlık Token', () => {
  test('Reset Password sekmesinde 000000 OTP ile yeni şifre belirlenebilmelidir', async ({ page }) => {
    const oldPassword = 'OldPassword123!';
    const newPassword = 'NewPassword456!';

    // 1. Yeni hesap oluştur
    const email = await registerAndLogin(page, { password: oldPassword });

    // 2. Çıkış yap
    await logoutUser(page);

    // 3. Reset Password sekmesine geç
    await page.getByTestId('01401-tab-reset').or(page.getByRole('button', { name: /Reset password|Şifre Sıfırla/i })).click();

    // 4. Form doldur
    await page.getByTestId('01401-email-input').or(page.locator('input[type="email"]')).fill(email);
    await page.getByTestId('01401-otp-input').or(page.locator('input[placeholder*="000000" i]')).fill(MASTER_OTP);
    await page.getByTestId('01402-password-input').or(page.locator('input[type="password"]')).fill(newPassword);

    // 5. Reset butonuna tıkla
    await page.locator('form button[type="submit"], form .primary-button').last().click();

    // 6. Başarı mesajı veya Sign In'e yönlenme bekleniyor (Uygulama başarı sonrası mode='signin' yapıyor)
    await expect(page.getByTestId('01101-tab-signin').or(page.locator('.auth-mode-tab.active'))).toBeVisible({ timeout: 6000 });

    // 7. Yeni şifre ile giriş yap
    await page.getByTestId('01101-email-input').or(page.locator('input[type="email"]')).fill(email);
    await page.getByTestId('01101-password-input').or(page.locator('input[type="password"]')).fill(newPassword);
    await page.getByTestId('01101-submit-btn').or(page.getByRole('button', { name: /Sign in|Giriş/i })).last().click();

    // 8. Dashboard'a ulaşıldı mı doğrula
    await expect(page.locator('.app-shell')).toBeVisible({ timeout: 10000 });
  });
});

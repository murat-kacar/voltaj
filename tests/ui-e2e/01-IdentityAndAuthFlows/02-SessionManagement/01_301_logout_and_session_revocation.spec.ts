/**
 * 01_301_logout_and_session_revocation.spec.ts
 * VUT: 01.3.01 — Oturum İptali (Session Revocation) & Güvenli Çıkış
 *
 * Senaryo:
 *   1. Admin olarak giriş yap
 *   2. Dashboard'da kullanıcı chip'ine (sidebar footer) tıkla
 *   3. Token localStorage'dan siliniyor mu kontrol et
 *   4. Auth ekranına dönüldüğünü ve korumalı bileşenlerin kaldırıldığını doğrula
 */

import { test, expect } from '@playwright/test';
import { loginAsAdmin } from '../../test-helpers/auth';

test.describe('01-IdentityAndAuthFlows > Senaryo 1.3: Oturum İptali (Session Revocation)', () => {
  test('Kullanıcı çıkış yaptığında token temizlenmeli ve auth ekranına dönmelidir', async ({ page }) => {
    // 1. Admin olarak giriş yap
    await loginAsAdmin(page);

    // 2. Dashboard görünüyor mu doğrula
    await expect(page.locator('.sidebar')).toBeVisible({ timeout: 6000 });

    // 3. localStorage'da session var mı kontrol et
    const sessionBefore = await page.evaluate(() => localStorage.getItem('voltflow.session'));
    expect(sessionBefore).not.toBeNull();

    // 4. Sidebar footer'daki kullanıcı chip'ine tıkla (logout butonu)
    const userChip = page.locator('.user-chip').last();
    await expect(userChip).toBeVisible({ timeout: 5000 });
    await userChip.click();

    // 5. localStorage temizlendi mi kontrol et
    await expect.poll(async () => {
      return await page.evaluate(() => localStorage.getItem('voltflow.session'));
    }).toBeNull();

    // 6. Auth page görünüyor ve sidebar kalkmış mı doğrula
    await expect(page.locator('.auth-page')).toBeVisible({ timeout: 5000 });
    await expect(page.locator('.sidebar')).not.toBeVisible();
  });
});

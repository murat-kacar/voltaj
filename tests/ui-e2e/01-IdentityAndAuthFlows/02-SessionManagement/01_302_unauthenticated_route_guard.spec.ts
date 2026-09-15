/**
 * 01_302_unauthenticated_route_guard.spec.ts
 * VUT: 01.3.02 — Oturum Yokken Korumalı Alanlara Erişim Engeli
 *
 * Senaryo:
 *   Oturum açılmamışken korumalı sayfaya erişilmek istendiğinde auth/login ekranı sunulmalı ve sidebar gizlenmelidir.
 */

import { test, expect } from '@playwright/test';

test.describe('01-IdentityAndAuthFlows > Senaryo 1.3.02: Oturumsuz Erişim Koruması', () => {
  test('Çıkış sonrası veya oturumsuz erişimde login/auth ekranı sunulmalıdır', async ({ page }) => {
    await page.goto('/');
    // localStorage'da session temizle
    await page.evaluate(() => localStorage.removeItem('voltflow.session'));
    await page.reload();

    // localStorage'da session olmadığından auth sayfası gösterilmeli
    await expect(page.locator('.auth-page')).toBeVisible({ timeout: 6000 });
    await expect(page.locator('input[type="email"], input[data-testid="01101-email-input"]')).toBeVisible();
    await expect(page.locator('.sidebar')).not.toBeVisible();
  });
});

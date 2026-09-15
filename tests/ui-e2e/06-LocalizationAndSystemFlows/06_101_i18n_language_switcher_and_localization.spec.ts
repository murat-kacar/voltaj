/**
 * 06_101_i18n_language_switcher_and_localization.spec.ts
 * VUT: 06.1.01 / Halka 17 — Auth Ekranı i18n Dil Değiştirici ve Yerelleştirme
 *
 * Senaryo:
 *   1. Auth ekranında dil değiştirici butonuna tıkla (EN <-> TR)
 *   2. Başlık, sekme ve form etiketlerinin anında Türkçe/İngilizce değiştiğini doğrula
 */

import { test, expect } from '@playwright/test';

test.describe('06-LocalizationAndSystemFlows > Senaryo 6.1.01: Auth Ekranı i18n Dil Değiştirici', () => {
  test('Auth ekranında dil değiştirildiğinde arayüz metinleri dinamik olarak güncellenmelidir', async ({ page }) => {
    await page.goto('/');

    // Auth ekranı görünür olmalı
    await expect(page.locator('.auth-page')).toBeVisible({ timeout: 6000 });

    const langBtn = page.locator('.lang-switcher-btn').first();
    await expect(langBtn).toBeVisible();

    // Mevcut buton metnini kontrol et ve dili değiştir
    const currentText = await langBtn.textContent();
    await langBtn.click();

    // TR moduna geçildiğinde "Giriş Yap" veya "Hesap oluştur" görünmeli
    if (currentText?.includes('EN')) {
      await expect(page.locator('.auth-card')).toContainText(/Tekrar hoş geldiniz|Giriş yap|Hesap oluştur/i);
    } else {
      await expect(page.locator('.auth-card')).toContainText(/Welcome back|Sign in|Create account/i);
    }
  });
});

/**
 * 06_102_dashboard_i18n_language_switcher.spec.ts
 * VUT: 06.1.02 / Halka 17 — Dashboard i18n Dil Değiştirici ve Dinamik Navigasyon
 *
 * Senaryo:
 *   1. Admin girişi yap
 *   2. Dashboard topbar'ındaki dil değiştiriciye tıkla (EN <-> TR)
 *   3. Navigasyon menüsü etiketlerinin dinamik güncellendiğini doğrula
 */

import { test, expect } from '@playwright/test';
import { loginAsAdmin } from '../test-helpers/auth';

test.describe('06-LocalizationAndSystemFlows > Senaryo 6.1.02: Dashboard i18n Dil Değiştirici', () => {
  test('Dashboard üzerinde dil değiştirildiğinde navigasyon ve başlıklar güncellenmelidir', async ({ page }) => {
    await loginAsAdmin(page);

    // Dashboard üzerindeki dil butonuna tıkla
    const topLangBtn = page.locator('.top-actions .lang-switcher-btn');
    await expect(topLangBtn).toBeVisible({ timeout: 6000 });

    const initialLangText = await topLangBtn.textContent();
    await topLangBtn.click();

    // Navigasyon menüsü kontrolü
    if (initialLangText?.includes('EN')) {
      // Türkçe'ye dönüştü -> 'Müşteriler' veya 'İş Emirleri' olmalı
      await expect(page.locator('.primary-nav')).toContainText(/Müşteriler|İş Emirleri|Genel Bakış/i);
    } else {
      // İngilizce'ye dönüştü -> 'Customers' veya 'Work orders' olmalı
      await expect(page.locator('.primary-nav')).toContainText(/Customers|Work orders|Overview/i);
    }
  });
});

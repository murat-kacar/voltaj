/**
 * 02_301_empty_quote_issue_blocked.spec.ts
 * VUT: 02.3.01 — Kalemsiz Teklif Yayınlama Engeli
 *
 * Senaryo:
 *   1. Müşteri oluştur
 *   2. Teklif oluştur (kalem eklemeden)
 *   3. Teklifi listede bul, detay drawer'ını aç
 *   4. "Issue Quote" butonuna tıkla
 *   5. API 422 döner → drawer'da error panel görünür
 *   6. Teklif hâlâ Draft durumunda kalır
 */

import { test, expect } from '@playwright/test';
import { loginAsAdmin, navigateTo, createTestCustomerApi } from '../../test-helpers/auth';

test.describe('02-QuoteToOrderFlows > Senaryo 2.3: Kalemsiz Teklif Yayınlama Engeli', () => {
  test('Kalemsiz teklif yayınlanmaya çalışıldığında engellenmeli ve Draft kalmalıdır', async ({ page }) => {
    await loginAsAdmin(page);

    // 1. Önce müşteri oluştur
    const customer = await createTestCustomerApi(page, `Müşteri Kalemsiz ${Date.now()}`);

    // 2. Quotes sayfasına git, yeni teklif oluştur
    await navigateTo(page, 'Quotes');
    await expect(page.getByTestId('02301-new-quote-btn')).toBeVisible({ timeout: 5000 });
    await page.getByTestId('02301-new-quote-btn').click();

    await expect(page.locator('.form-modal')).toBeVisible({ timeout: 4000 });

    // Müşteri seç
    const customerSelect = page.getByTestId('02301-customer-select');
    await expect(customerSelect).toBeVisible();
    await customerSelect.selectOption({ value: customer.id }).catch(async () => {
      const count = await customerSelect.locator('option').count();
      await customerSelect.selectOption({ index: count - 1 });
    });

    // Başlık doldur
    const quoteTitle = `Kalemsiz Teklif ${Date.now()}`;
    await page.getByTestId('02301-title-input').fill(quoteTitle);
    await page.getByTestId('02301-submit-btn').click();

    // Modal kapandı mı?
    await expect(page.locator('.form-modal')).not.toBeVisible({ timeout: 6000 });

    // 3. Oluşturulan teklifi listede bul ve aç
    const quoteRow = page.locator('.quote-row').filter({ hasText: quoteTitle }).or(page.locator('.quote-row').first());
    await expect(quoteRow.first()).toBeVisible({ timeout: 6000 });
    await quoteRow.first().click();

    // 4. Drawer açıldı, Draft durumunda
    await expect(page.locator('.drawer.open')).toBeVisible({ timeout: 4000 });
    await expect(page.locator('.drawer.open').getByText('Draft')).toBeVisible();

    // 5. "Issue Quote" butonuna tıkla
    await expect(page.getByTestId('02301-issue-btn')).toBeVisible({ timeout: 3000 });
    await page.getByTestId('02301-issue-btn').click();

    // 6. Hata paneli görünmeli (422 — kalemsiz teklif yayınlanamaz)
    await expect(page.getByTestId('02401-problem-details').or(page.locator('.error-state, .problem-details'))).toBeVisible({ timeout: 6000 });

    // 7. Teklif hâlâ Draft kalmalı
    await expect(page.locator('.drawer.open').getByText('Draft')).toBeVisible();
  });
});

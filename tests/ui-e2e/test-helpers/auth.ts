/**
 * test-helpers/auth.ts
 * Voltflow UI-E2E — Paylaşımlı Kimlik Doğrulama Yardımcıları
 *
 * Tüm spec dosyaları bu modülü kullanarak auth akışlarını tekrarlamaktan kaçınır.
 * Admin kullanıcısı her test koşumundan önce API seed ile mevcut olmalıdır.
 */

import { type Page, expect } from '@playwright/test';

/** Admin hesabı (API seed ile oluşturulan varsayılan yönetici) */
export const ADMIN_EMAIL = 'admin@voltflow.com';
export const ADMIN_PASSWORD = 'Admin123!';

/** Master OTP — anında onay verir (test ortamı) */
export const MASTER_OTP = '000000';

/** Frontend base URL */
export const BASE_URL = 'http://localhost:5173';

/** API base URL */
export const API_URL = '/api';

/**
 * Admin kullanıcısı olarak giriş yapar.
 * Başarılı girişten sonra page, dashboard URL'inde olacaktır.
 */
export async function loginAsAdmin(page: Page): Promise<void> {
  await page.goto('/');
  await expect(page).toHaveTitle(/Voltflow/i);

  // Zaten giriş yapılmışsa sidebar kontrolü
  if (await page.locator('.app-shell').isVisible({ timeout: 1000 }).catch(() => false)) {
    return;
  }

  // Sign In sekmesini seç
  const signInTab = page.getByTestId('01101-tab-signin');
  if (await signInTab.isVisible({ timeout: 1500 }).catch(() => false)) {
    await signInTab.click();
  }

  const emailInput = page.getByTestId('01101-email-input').or(page.locator('input[type="email"]')).first();
  const passwordInput = page.getByTestId('01101-password-input').or(page.locator('input[type="password"]')).first();
  const submitBtn = page.getByTestId('01101-submit-btn').or(page.getByRole('button', { name: /Sign in/i })).last();

  await emailInput.fill(ADMIN_EMAIL);
  await passwordInput.fill(ADMIN_PASSWORD);
  await submitBtn.click();

  // Dashboard'a geçişi bekle
  await expect(page.locator('.app-shell')).toBeVisible({ timeout: 10000 });
}

/**
 * Dinamik e-posta ile yeni bir kullanıcı kaydeder ve giriş yapar.
 * Master OTP (000000) kullanarak anında onay verir.
 * @returns Oluşturulan kullanıcının e-posta adresi
 */
export async function registerAndLogin(
  page: Page,
  options: { name?: string; password?: string } = {}
): Promise<string> {
  const uniqueEmail = `e2e_${Date.now()}_${Math.floor(Math.random() * 10000)}@voltflow.com`;
  const name = options.name ?? 'E2E Test User';
  const password = options.password ?? 'TestPassword123!';

  await page.goto('/');

  // Register sekmesine geç
  const registerTab = page.getByTestId('01201-tab-register').or(page.getByRole('button', { name: /Create account|Hesap oluştur/i })).first();
  await registerTab.click();

  const nameInput = page.getByTestId('01201-name-input').or(page.locator('input[placeholder*="name" i]')).first();
  const emailInput = page.getByTestId('01201-email-input').or(page.locator('input[type="email"]')).first();
  const passwordInput = page.getByTestId('01201-password-input').or(page.locator('input[type="password"]')).first();
  const otpInput = page.getByTestId('01201-otp-input').or(page.locator('input[placeholder*="OTP" i], input[placeholder*="000000" i]')).first();
  const submitBtn = page.getByTestId('01201-submit-btn').or(page.getByRole('button', { name: /Register|Create account/i })).last();

  await nameInput.fill(name);
  await emailInput.fill(uniqueEmail);
  await passwordInput.fill(password);
  await otpInput.fill(MASTER_OTP);

  await submitBtn.click();

  // Otomatik login veya manuel login kontrolü
  const signInBtn = page.getByTestId('01101-submit-btn').or(page.getByRole('button', { name: /Sign in/i })).last();
  if (await signInBtn.isVisible({ timeout: 2000 }).catch(() => false)) {
    const signInEmail = page.getByTestId('01101-email-input').or(page.locator('input[type="email"]')).first();
    const signInPass = page.getByTestId('01101-password-input').or(page.locator('input[type="password"]')).first();
    await signInEmail.fill(uniqueEmail);
    await signInPass.fill(password);
    await signInBtn.click();
  }

  await expect(page.locator('.app-shell')).toBeVisible({ timeout: 10000 });

  return uniqueEmail;
}

/**
 * Oturumu kapatır (Sidebar footer user-chip tıklayarak)
 */
export async function logoutUser(page: Page): Promise<void> {
  const userChip = page.locator('.user-chip').last();
  await expect(userChip).toBeVisible({ timeout: 5000 });
  await userChip.click();
  await expect(page.locator('.auth-page')).toBeVisible({ timeout: 8000 });
}

/**
 * Sidebar'da belirtilen navigation item'ına tıklar.
 * @param label Navigasyon etiketi (örn: 'Customers', 'Work orders', 'Quotes', 'Inventory', 'Payments')
 */
export async function navigateTo(page: Page, label: string): Promise<void> {
  const navBtn = page.locator('.primary-nav .nav-item').filter({ hasText: new RegExp(label, 'i') }).first();
  await expect(navBtn).toBeVisible({ timeout: 5000 });
  await navBtn.click();
  await page.waitForTimeout(200);
}

/**
 * API üzerinden test müşterisi oluşturur.
 */
export async function createTestCustomerApi(page: Page, customName?: string): Promise<{ id: string; fullName: string; email: string }> {
  const name = customName ?? `E2E Customer ${Date.now()}`;
  const email = `customer_${Date.now()}_${Math.floor(Math.random() * 1000)}@voltflow-test.com`;
  const result = await page.evaluate(async ({ name, email, apiUrl }) => {
    const session = JSON.parse(localStorage.getItem('voltflow.session') || '{}');
    const token = session.token;
    const res = await fetch(`${apiUrl}/customers`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        ...(token ? { Authorization: `Bearer ${token}` } : {})
      },
      body: JSON.stringify({ fullName: name, email, phone: '+90 532 555 0199' })
    });
    if (!res.ok) {
      const err = await res.text();
      throw new Error(`Customer API creation failed: ${err}`);
    }
    return res.json();
  }, { name, email, apiUrl: API_URL });

  return result;
}

/**
 * API üzerinden test teklifi oluşturur.
 */
export async function createTestQuoteApi(page: Page, customerId: string, title?: string): Promise<{ id: string; number: string; title: string }> {
  const quoteTitle = title ?? `E2E Quote ${Date.now()}`;
  const result = await page.evaluate(async ({ customerId, quoteTitle, apiUrl }) => {
    const session = JSON.parse(localStorage.getItem('voltflow.session') || '{}');
    const token = session.token;
    const res = await fetch(`${apiUrl}/quotes`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        ...(token ? { Authorization: `Bearer ${token}` } : {})
      },
      body: JSON.stringify({ customerId, title: quoteTitle })
    });
    if (!res.ok) {
      const err = await res.text();
      throw new Error(`Quote API creation failed: ${err}`);
    }
    return res.json();
  }, { customerId, quoteTitle, apiUrl: API_URL });

  return result;
}

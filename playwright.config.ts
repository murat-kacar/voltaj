import { defineConfig, devices } from '@playwright/test';

export default defineConfig({
  testDir: './tests/ui-e2e',
  fullyParallel: false,
  workers: 1,
  timeout: 30000,
  expect: {
    timeout: 8000,
  },
  reporter: [['list'], ['html', { open: 'never' }]],
  use: {
    baseURL: 'http://localhost:5173',
    trace: 'on-first-retry',
    video: 'on',
    screenshot: 'on',
    actionTimeout: 10000,
  },
  webServer: {
    command: 'npm run dev --prefix frontend',
    url: 'http://localhost:5173',
    reuseExistingServer: true,
    timeout: 30000,
  },
  projects: [
    {
      name: 'chromium',
      use: { ...devices['Desktop Chrome'] },
    },
  ],
});

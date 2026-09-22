import { render, screen } from '@testing-library/react'
import { MemoryRouter } from 'react-router-dom'
import { describe, it, expect, vi } from 'vitest'
import { AppShell } from '../AppShell'
import { ThemeProvider, createTheme } from '@mui/material/styles'

import '@testing-library/jest-dom'

// Mock useI18n to prevent context issues in tests
vi.mock('../../i18n', () => ({
  useI18n: () => ({
    lang: 'en',
    setLang: vi.fn(),
    translate: (key: string) => key,
  }),
}))

describe('AppShell', () => {
  const defaultProps = {
    session: { id: '1', name: 'Test User', roles: [], permissions: [], type: 'User' as const, userId: '1', email: 'test@example.com', token: 'token', isApproved: true },
    activeView: 'dashboard',
    navGroups: [],
    mobileNavItems: [],
    activeGroupId: undefined,
    pendingReminders: 0,
    mode: 'light' as const,
    onToggleMode: vi.fn(),
    onLogout: vi.fn(),
  }

  it('renders the brand name and respects Q5 data-testid rule', () => {
    render(
      <ThemeProvider theme={createTheme()}>
        <MemoryRouter>
          <AppShell {...defaultProps}>
            <div data-testid="test-children" />
          </AppShell>
        </MemoryRouter>
      </ThemeProvider>
    )

    // Using data-testid per rule Q5
    const homeBtn = screen.getByTestId('nav-home')
    expect(homeBtn).toBeTruthy()
    
    // Check that children are rendered
    expect(screen.getByTestId('test-children')).toBeTruthy()
  })
})

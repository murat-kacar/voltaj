import { useState } from 'react'
import type { FormEvent } from 'react'
import { authApi, ApiError } from './api'
import type { AuthResult, ProblemDetails } from './api'
import { useI18n } from './i18n'

type AuthViewProps = {
  onAuthenticated: (session: AuthResult) => void
}

type AuthMode = 'signin' | 'register' | 'reset'

export function AuthView({ onAuthenticated }: AuthViewProps) {
  const { lang, setLang, t } = useI18n()
  const [mode, setMode] = useState<AuthMode>('signin')

  // Form states
  const [name, setName] = useState('')
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [otp, setOtp] = useState('000000')

  // Status states
  const [error, setError] = useState('')
  const [problem, setProblem] = useState<ProblemDetails | null>(null)
  const [successMessage, setSuccessMessage] = useState('')
  const [pending, setPending] = useState(false)
  const [submitting, setSubmitting] = useState(false)

  function switchMode(newMode: AuthMode) {
    setMode(newMode)
    setError('')
    setProblem(null)
    setSuccessMessage('')
  }

  async function handleLogin(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    setError('')
    setProblem(null)
    setSubmitting(true)
    try {
      const result = await authApi.login(email.trim(), password)
      if (!result.isApproved) {
        setPending(true)
        return
      }
      localStorage.setItem('voltflow.session', JSON.stringify(result))
      onAuthenticated(result)
    } catch (err: unknown) {
      if (err instanceof ApiError && err.problemDetails) {
        setProblem(err.problemDetails)
        setError(err.problemDetails.detail || err.problemDetails.title || err.message)
      } else {
        setError(err instanceof Error ? err.message : 'Unable to sign in.')
      }
    } finally {
      setSubmitting(false)
    }
  }

  async function handleRegister(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    setError('')
    setProblem(null)
    setSubmitting(true)
    try {
      const result = await authApi.register({
        name: name.trim(),
        email: email.trim(),
        password,
        otp: otp.trim(),
      })

      if (result.isApproved && result.token) {
        // Auto-login with OTP 000000!
        localStorage.setItem('voltflow.session', JSON.stringify(result))
        onAuthenticated(result)
      } else {
        setPending(true)
      }
    } catch (err: unknown) {
      if (err instanceof ApiError && err.problemDetails) {
        setProblem(err.problemDetails)
        setError(err.problemDetails.detail || err.problemDetails.title || err.message)
      } else {
        setError(err instanceof Error ? err.message : 'Registration failed.')
      }
    } finally {
      setSubmitting(false)
    }
  }

  async function handleResetPassword(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    setError('')
    setProblem(null)
    setSubmitting(true)
    try {
      await authApi.completePasswordReset({
        token: otp.trim(),
        newPassword: password,
        email: email.trim(),
      })
      setSuccessMessage('Password successfully updated! You can now sign in with your new password.')
      setPassword('')
      setMode('signin')
    } catch (err: unknown) {
      if (err instanceof ApiError && err.problemDetails) {
        setProblem(err.problemDetails)
        setError(err.problemDetails.detail || err.problemDetails.title || err.message)
      } else {
        setError(err instanceof Error ? err.message : 'Password reset failed.')
      }
    } finally {
      setSubmitting(false)
    }
  }

  if (pending) {
    return (
      <main className="auth-page">
        <section className="auth-card">
          <div className="auth-brand">
            <span>V</span>
            <b>voltflow</b>
          </div>
          <div className="pending-mark">✓</div>
          <p className="eyebrow">Account registered</p>
          <h1>Approval pending</h1>
          <p className="auth-copy">
            Your account was registered. If you did not use the instant Test OTP (<code>000000</code>), an administrator must approve your account.
          </p>
          <button className="secondary-button" onClick={() => { setPending(false); setMode('signin') }}>
            Back to sign in
          </button>
        </section>
      </main>
    )
  }

  return (
    <main className="auth-page" data-screen-id="SCR-0110">
      <section className="auth-card">
        <div className="auth-brand" style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
          <div style={{ display: 'flex', alignItems: 'center', gap: '10px' }}>
            <span>V</span>
            <b>voltflow</b>
          </div>
          <button
            type="button"
            className="lang-switcher-btn"
            onClick={() => setLang(lang === 'en' ? 'tr' : 'en')}
            title={lang === 'en' ? 'Türkçe arayüze geç' : 'Switch to English (USA)'}
          >
            {lang === 'en' ? '🇺🇸 EN (US)' : '🇹🇷 TR'}
          </button>
        </div>

        <div className="auth-mode-switch">
          <button
            type="button"
            className={`auth-mode-tab ${mode === 'signin' ? 'active' : ''}`}
            data-testid="01101-tab-signin"
            onClick={() => switchMode('signin')}
          >
            {t.signIn}
          </button>
          <button
            type="button"
            className={`auth-mode-tab ${mode === 'register' ? 'active' : ''}`}
            data-testid="01201-tab-register"
            onClick={() => switchMode('register')}
          >
            {lang === 'tr' ? 'Hesap oluştur' : 'Create account'}
          </button>
          <button
            type="button"
            className={`auth-mode-tab ${mode === 'reset' ? 'active' : ''}`}
            data-testid="01401-tab-reset"
            onClick={() => switchMode('reset')}
          >
            {t.resetPassword}
          </button>
        </div>

        {mode === 'signin' && (
          <>
            <p className="eyebrow">{lang === 'tr' ? 'Çalışan Çalışma Alanı' : 'Employee workspace'}</p>
            <h1>{lang === 'tr' ? 'Tekrar hoş geldiniz.' : 'Welcome back.'}</h1>
            <p className="auth-copy">
              {lang === 'tr'
                ? 'Müşterileri, iş emirlerini ve operasyonları yönetmek için giriş yapın.'
                : 'Sign in to manage customers, work orders and operations.'}
            </p>
          </>
        )}

        {mode === 'register' && (
          <>
            <p className="eyebrow">Instant onboarding</p>
            <h1>Create an account</h1>
            <p className="auth-copy">
              Use test OTP <code>000000</code> to automatically verify and activate your profile.
            </p>
          </>
        )}

        {mode === 'reset' && (
          <>
            <p className="eyebrow">Account recovery</p>
            <h1>Reset password</h1>
            <p className="auth-copy">
              Enter your email and test OTP <code>000000</code> to immediately assign a new password.
            </p>
          </>
        )}

        {successMessage && <div className="auth-alert success-alert">{successMessage}</div>}

        {error && (
          <div className="problem-details" data-testid="auth-problem-details">
            <strong>{problem?.title ?? 'Operation Failed'}</strong>
            <span>{error}</span>
            {problem?.traceId && <small>Trace: {problem.traceId}</small>}
          </div>
        )}

        {/* 1. SIGN IN FORM */}
        {mode === 'signin' && (
          <form onSubmit={handleLogin}>
            <label>
              Email
              <input
                type="email"
                data-testid="01101-email-input"
                value={email}
                onChange={(event) => setEmail(event.target.value)}
                placeholder="admin@voltflow.com"
                required
                autoFocus
                disabled={submitting}
              />
            </label>
            <label>
              <div className="label-with-action">
                <span>Password</span>
                <button
                  type="button"
                  className="link-button"
                  onClick={() => switchMode('reset')}
                  tabIndex={-1}
                >
                  Forgot?
                </button>
              </div>
              <input
                type="password"
                data-testid="01101-password-input"
                value={password}
                onChange={(event) => setPassword(event.target.value)}
                placeholder="Your password"
                required
                disabled={submitting}
              />
            </label>
            <button className="primary-button auth-submit" data-testid="01101-submit-btn" disabled={submitting}>
              {submitting ? 'Signing in…' : 'Sign in'} <span>→</span>
            </button>
          </form>
        )}

        {/* 2. REGISTER FORM */}
        {mode === 'register' && (
          <form onSubmit={handleRegister}>
            <label>
              Full name
              <input
                type="text"
                data-testid="01201-name-input"
                value={name}
                onChange={(event) => setName(event.target.value)}
                placeholder="e.g. Canberk Demir"
                required
                autoFocus
                disabled={submitting}
              />
            </label>
            <label>
              Email address
              <input
                type="email"
                data-testid="01201-email-input"
                value={email}
                onChange={(event) => setEmail(event.target.value)}
                placeholder="user@voltflow.com"
                required
                disabled={submitting}
              />
            </label>
            <label>
              Password (min 8 chars)
              <input
                type="password"
                data-testid="01201-password-input"
                value={password}
                onChange={(event) => setPassword(event.target.value)}
                placeholder="Secure password"
                minLength={8}
                required
                disabled={submitting}
              />
            </label>
            <label>
              <div className="label-with-action">
                <span>Verification OTP</span>
                <span className="badge-hint">Fixed: 000000</span>
              </div>
              <input
                type="text"
                data-testid="01201-otp-input"
                value={otp}
                onChange={(event) => setOtp(event.target.value)}
                placeholder="000000"
                maxLength={6}
                required
                disabled={submitting}
              />
            </label>
            <button className="primary-button auth-submit" data-testid="01201-submit-btn" disabled={submitting}>
              {submitting ? 'Creating account…' : 'Register & Launch'} <span>→</span>
            </button>
          </form>
        )}

        {/* 3. RESET PASSWORD FORM */}
        {mode === 'reset' && (
          <form onSubmit={handleResetPassword}>
            <label>
              Registered Email
              <input
                type="email"
                data-testid="01401-email-input"
                value={email}
                onChange={(event) => setEmail(event.target.value)}
                placeholder="user@voltflow.com"
                required
                autoFocus
                disabled={submitting}
              />
            </label>
            <label>
              <div className="label-with-action">
                <span>Recovery OTP</span>
                <span className="badge-hint">Fixed: 000000</span>
              </div>
              <input
                type="text"
                data-testid="01401-otp-input"
                value={otp}
                onChange={(event) => setOtp(event.target.value)}
                placeholder="000000"
                maxLength={6}
                required
                disabled={submitting}
              />
            </label>
            <label>
              New Password (min 8 chars)
              <input
                type="password"
                data-testid="01402-password-input"
                value={password}
                onChange={(event) => setPassword(event.target.value)}
                placeholder="New secure password"
                minLength={8}
                required
                disabled={submitting}
              />
            </label>
            <button className="primary-button auth-submit" data-testid="01401-submit-btn" disabled={submitting}>
              {submitting ? 'Updating password…' : 'Set New Password'} <span>→</span>
            </button>
          </form>
        )}

        <div className="auth-footer-help">
          {mode === 'signin' ? (
            <p className="auth-footnote">
              Need a test account?{' '}
              <button type="button" className="inline-link" onClick={() => switchMode('register')}>
                Register with OTP 000000
              </button>
            </p>
          ) : (
            <p className="auth-footnote">
              Already have an account?{' '}
              <button type="button" className="inline-link" onClick={() => switchMode('signin')}>
                Sign in here
              </button>
            </p>
          )}
        </div>
      </section>

      <aside className="auth-aside">
        <div className="aside-orbit orbit-one" />
        <div className="aside-orbit orbit-two" />
        <p className="eyebrow">Operations, in focus</p>
        <h2>Turn a busy day into a clear queue.</h2>
        <p>One place for the work that matters, from the first quote to the final payment.</p>
        <div className="aside-rule" />
        <small>Customer · Quote · Work order · Inventory</small>
      </aside>
    </main>
  )
}

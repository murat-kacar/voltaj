import { useState } from 'react'
import type { FormEvent } from 'react'
import { authApi, ApiError } from './api'
import type { AuthResult, ProblemDetails } from './api'
import { useTranslation, translateApiError } from './i18n'

type AuthViewProps = {
  onAuthenticated: (session: AuthResult) => void
}

type AuthMode = 'signin' | 'register' | 'reset'

export function AuthView({ onAuthenticated }: AuthViewProps) {
  const { t, i18n } = useTranslation(['auth', 'common', 'errors'])
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

  const lang = i18n.language === 'tr' ? 'tr' : 'en'

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
        setError(translateApiError(err.problemDetails))
      } else {
        setError(err instanceof Error ? err.message : t('auth:messages.loginFailed'))
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
        setError(translateApiError(err.problemDetails))
      } else {
        setError(err instanceof Error ? err.message : t('auth:messages.registerFailed'))
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
      setSuccessMessage(t('auth:messages.resetSuccess'))
      setPassword('')
      setMode('signin')
    } catch (err: unknown) {
      if (err instanceof ApiError && err.problemDetails) {
        setProblem(err.problemDetails)
        setError(translateApiError(err.problemDetails))
      } else {
        setError(err instanceof Error ? err.message : t('auth:messages.resetFailed'))
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
            <b>{t('auth:brand')}</b>
          </div>
          <div className="pending-mark">✓</div>
          <p className="eyebrow">{t('auth:approvalPending.eyebrow')}</p>
          <h1>{t('auth:approvalPending.title')}</h1>
          <p className="auth-copy">
            {t('auth:approvalPending.copy')}
          </p>
          <button className="secondary-button" onClick={() => { setPending(false); setMode('signin') }}>
            {t('auth:approvalPending.backToSignIn')}
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
            <b>{t('auth:brand')}</b>
          </div>
          <button
            type="button"
            className="lang-switcher-btn"
            onClick={() => i18n.changeLanguage(lang === 'en' ? 'tr' : 'en')}
            title={lang === 'en' ? 'Türkçe arayüze geç' : 'Switch to English'}
          >
            {lang === 'en' ? t('common:lang.tr') : t('common:lang.en')}
          </button>
        </div>

        <div className="auth-mode-switch">
          <button
            type="button"
            className={`auth-mode-tab ${mode === 'signin' ? 'active' : ''}`}
            data-testid="01101-tab-signin"
            onClick={() => switchMode('signin')}
          >
            {t('auth:signIn')}
          </button>
          <button
            type="button"
            className={`auth-mode-tab ${mode === 'register' ? 'active' : ''}`}
            data-testid="01201-tab-register"
            onClick={() => switchMode('register')}
          >
            {t('auth:register')}
          </button>
          <button
            type="button"
            className={`auth-mode-tab ${mode === 'reset' ? 'active' : ''}`}
            data-testid="01401-tab-reset"
            onClick={() => switchMode('reset')}
          >
            {t('auth:reset')}
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
            <p className="eyebrow">{lang === 'tr' ? 'Hızlı Katılım' : 'Instant onboarding'}</p>
            <h1>{t('auth:actions.registerButton')}</h1>
            <p className="auth-copy">
              {lang === 'tr'
                ? 'Hesabınızı otomatik olarak doğrulamak ve etkinleştirmek için test OTP 000000 kodunu kullanın.'
                : 'Use test OTP 000000 to automatically verify and activate your profile.'}
            </p>
          </>
        )}

        {mode === 'reset' && (
          <>
            <p className="eyebrow">{lang === 'tr' ? 'Hesap Kurtarma' : 'Account recovery'}</p>
            <h1>{t('auth:reset')}</h1>
            <p className="auth-copy">
              {lang === 'tr'
                ? 'Yeni bir şifre atamak için e-postanızı ve 000000 test OTP kodunu girin.'
                : 'Enter your email and test OTP 000000 to immediately assign a new password.'}
            </p>
          </>
        )}

        {successMessage && <div className="auth-alert success-alert">{successMessage}</div>}

        {error && (
          <div className="problem-details" data-testid="auth-problem-details">
            <strong>{problem?.title ?? t('errors:general.unexpectedError')}</strong>
            <span>{error}</span>
            {problem?.traceId && <small>Trace: {problem.traceId}</small>}
          </div>
        )}

        {/* 1. SIGN IN FORM */}
        {mode === 'signin' && (
          <form onSubmit={handleLogin}>
            <label>
              {t('auth:fields.email')}
              <input
                type="email"
                data-testid="01101-email-input"
                value={email}
                onChange={(event) => setEmail(event.target.value)}
                placeholder={t('auth:fields.emailPlaceholder')}
                required
                autoFocus
                disabled={submitting}
              />
            </label>
            <label>
              <div className="label-with-action">
                <span>{t('auth:fields.password')}</span>
                <button
                  type="button"
                  className="link-button"
                  onClick={() => switchMode('reset')}
                  tabIndex={-1}
                >
                  {t('auth:actions.forgotPassword')}
                </button>
              </div>
              <input
                type="password"
                data-testid="01101-password-input"
                value={password}
                onChange={(event) => setPassword(event.target.value)}
                placeholder={t('auth:fields.passwordPlaceholder')}
                required
                disabled={submitting}
              />
            </label>
            <button className="primary-button auth-submit" data-testid="01101-submit-btn" disabled={submitting}>
              {submitting ? t('auth:actions.signingIn') : t('auth:actions.signInButton')} <span>→</span>
            </button>
          </form>
        )}

        {/* 2. REGISTER FORM */}
        {mode === 'register' && (
          <form onSubmit={handleRegister}>
            <label>
              {t('auth:fields.fullName')}
              <input
                type="text"
                data-testid="01201-name-input"
                value={name}
                onChange={(event) => setName(event.target.value)}
                placeholder={t('auth:fields.fullNamePlaceholder')}
                required
                autoFocus
                disabled={submitting}
              />
            </label>
            <label>
              {t('auth:fields.email')}
              <input
                type="email"
                data-testid="01201-email-input"
                value={email}
                onChange={(event) => setEmail(event.target.value)}
                placeholder={t('auth:fields.emailPlaceholder')}
                required
                disabled={submitting}
              />
            </label>
            <label>
              {t('auth:fields.password')} (min 8 chars)
              <input
                type="password"
                data-testid="01201-password-input"
                value={password}
                onChange={(event) => setPassword(event.target.value)}
                placeholder={t('auth:fields.passwordPlaceholder')}
                minLength={8}
                required
                disabled={submitting}
              />
            </label>
            <label>
              <div className="label-with-action">
                <span>{t('auth:fields.masterOtp')}</span>
                <span className="badge-hint">{t('auth:fields.masterOtpHint')}</span>
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
              {submitting ? t('auth:actions.registering') : t('auth:actions.registerButton')} <span>→</span>
            </button>
          </form>
        )}

        {/* 3. RESET PASSWORD FORM */}
        {mode === 'reset' && (
          <form onSubmit={handleResetPassword}>
            <label>
              {t('auth:fields.email')}
              <input
                type="email"
                data-testid="01401-email-input"
                value={email}
                onChange={(event) => setEmail(event.target.value)}
                placeholder={t('auth:fields.emailPlaceholder')}
                required
                autoFocus
                disabled={submitting}
              />
            </label>
            <label>
              <div className="label-with-action">
                <span>{t('auth:fields.masterOtp')}</span>
                <span className="badge-hint">{t('auth:fields.masterOtpHint')}</span>
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
              {t('auth:fields.newPassword')} (min 8 chars)
              <input
                type="password"
                data-testid="01402-password-input"
                value={password}
                onChange={(event) => setPassword(event.target.value)}
                placeholder={t('auth:fields.passwordPlaceholder')}
                minLength={8}
                required
                disabled={submitting}
              />
            </label>
            <button className="primary-button auth-submit" data-testid="01401-submit-btn" disabled={submitting}>
              {submitting ? t('auth:actions.resetting') : t('auth:actions.resetButton')} <span>→</span>
            </button>
          </form>
        )}

        <div className="auth-footer-help">
          {mode === 'signin' ? (
            <p className="auth-footnote">
              {t('auth:actions.needAccount')}{' '}
              <button type="button" className="inline-link" onClick={() => switchMode('register')}>
                {t('auth:register')}
              </button>
            </p>
          ) : (
            <p className="auth-footnote">
              {t('auth:actions.haveAccount')}{' '}
              <button type="button" className="inline-link" onClick={() => switchMode('signin')}>
                {t('auth:signIn')}
              </button>
            </p>
          )}
        </div>
      </section>

      <aside className="auth-aside">
        <div className="aside-orbit orbit-one" />
        <div className="aside-orbit orbit-two" />
        <p className="eyebrow">{lang === 'tr' ? 'Operasyonlar Odakta' : 'Operations, in focus'}</p>
        <h2>{lang === 'tr' ? 'Yoğun bir günü net bir iş kuyruğuna dönüştürün.' : 'Turn a busy day into a clear queue.'}</h2>
        <p>
          {lang === 'tr'
            ? 'İlk tekliften son ödemeye kadar önemli olan tüm saha işleri için tek bir merkez.'
            : 'One place for the work that matters, from the first quote to the final payment.'}
        </p>
        <div className="aside-rule" />
        <small>{lang === 'tr' ? 'Müşteri · Teklif · İş Emri · Envanter' : 'Customer · Quote · Work order · Inventory'}</small>
      </aside>
    </main>
  )
}

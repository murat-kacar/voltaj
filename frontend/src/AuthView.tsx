import { useState } from 'react'
import { Container, Box, Typography, TextField, Button, Paper, Alert } from '@mui/material'
import { authApi, type AuthResult } from './api'
import { useI18n } from './i18n'

export function AuthView({ onAuthenticated }: { onAuthenticated: (res: AuthResult) => void }) {
  const { translate: t } = useI18n()
  const [isLogin, setIsLogin] = useState(true)
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [name, setName] = useState('')
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    setLoading(true)
    setError(null)

    try {
      if (isLogin) {
        const res = await authApi.login(email, password)
        localStorage.setItem('voltflow.session', JSON.stringify(res))
        onAuthenticated(res)
      } else {
        if (!name) throw new Error('Name is required')
        const res = await authApi.register({ email, password, name })
        localStorage.setItem('voltflow.session', JSON.stringify(res))
        onAuthenticated(res)
      }
    } catch (err: any) {
      setError(err.message || 'An error occurred')
    } finally {
      setLoading(false)
    }
  }

  return (
    <Container component="main" maxWidth="xs">
      <Box
        sx={{
          marginTop: 8,
          display: 'flex',
          flexDirection: 'column',
          alignItems: 'center',
        }}
      >
        <Paper elevation={3} sx={{ p: 4, width: '100%', borderRadius: 2 }}>
          <Typography component="h1" variant="h5" align="center" gutterBottom sx={{ fontWeight: 700, color: 'primary.main' }}>
            {t('common:brand.name')}
          </Typography>
          <Typography component="h2" variant="subtitle1" align="center" color="text.secondary" gutterBottom>
            {isLogin ? t('common:auth.title') : t('common:auth.register')}
          </Typography>
          
          {error && <Alert severity="error" sx={{ mb: 2 }}>{error}</Alert>}
          
          <Box component="form" onSubmit={handleSubmit} sx={{ mt: 1 }}>
            {!isLogin && (
              <TextField
                margin="normal"
                required
                fullWidth
                id="name"
                label={t('common:fields.name')}
                name="name"
                autoComplete="name"
                autoFocus
                value={name}
                onChange={e => setName(e.target.value)}
              />
            )}
            <TextField
              margin="normal"
              required
              fullWidth
              id="email"
              label={t('common:auth.email')}
              name="email"
              autoComplete="email"
              autoFocus={isLogin}
              value={email}
              onChange={e => setEmail(e.target.value)}
            />
            <TextField
              margin="normal"
              required
              fullWidth
              name="password"
              label={t('common:auth.password')}
              type="password"
              id="password"
              autoComplete="current-password"
              value={password}
              onChange={e => setPassword(e.target.value)}
            />
            
            <Button
              type="submit"
              fullWidth
              variant="contained"
              sx={{ mt: 3, mb: 2, py: 1.5 }}
              disabled={loading}
            >
              {loading ? '...' : (isLogin ? t('common:auth.login') : t('common:auth.register'))}
            </Button>
            
            <Button
              fullWidth
              variant="text"
              onClick={() => setIsLogin(!isLogin)}
            >
              {isLogin ? t('common:auth.noAccount') : t('common:auth.hasAccount')}
            </Button>
          </Box>
        </Paper>
      </Box>
    </Container>
  )
}

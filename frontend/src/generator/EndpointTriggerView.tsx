import { useState, useEffect, useMemo, useCallback } from 'react'
import {
  Box,
  Typography,
  Paper,
  Tabs,
  Tab,
  TextField,
  MenuItem,
  Button,
  Chip,
  Switch,
  FormControlLabel,
  Alert,
  Divider,
  Stack,
  Card,
  CardContent,
  IconButton,
  Tooltip,
  CircularProgress,
} from '@mui/material'
import PlayArrowIcon from '@mui/icons-material/PlayArrow'
import AutoFixHighIcon from '@mui/icons-material/AutoFixHigh'
import ContentCopyIcon from '@mui/icons-material/ContentCopy'
import CheckCircleIcon from '@mui/icons-material/CheckCircle'
import ClearIcon from '@mui/icons-material/Clear'
import TerminalIcon from '@mui/icons-material/Terminal'
import DataObjectIcon from '@mui/icons-material/DataObject'
import HttpIcon from '@mui/icons-material/Http'
import TimerIcon from '@mui/icons-material/Timer'

import {
  customersApi,
  usersApi,
  type Customer,
  type UserSummary,
  type AuthResult,
} from '../api'
import {
  ALL_ENDPOINTS,
  ENDPOINT_CATEGORIES,
  type EndpointDefinition,
  type EndpointField,
  type HttpMethod,
} from './endpointRegistry'
import { useI18n } from '../i18n'

interface CallHistoryItem {
  id: string
  method: HttpMethod
  url: string
  status: number
  statusText: string
  durationMs: number
  timestamp: string
  success: boolean
}

export function EndpointTriggerView() {
  const { translate: t } = useI18n()

  // State
  const [selectedCategory, setSelectedCategory] = useState<string>('all')
  const [selectedEndpointId, setSelectedEndpointId] = useState<string>('wo-create')

  // Form values
  const [pathValues, setPathValues] = useState<Record<string, string>>({})
  const [queryValues, setQueryValues] = useState<Record<string, string | number | boolean>>({})
  const [bodyValues, setBodyValues] = useState<Record<string, unknown>>({})

  // Execution & Response states
  const [executing, setExecuting] = useState<boolean>(false)
  const [responseStatus, setResponseStatus] = useState<{ code: number; text: string; success: boolean; duration: number } | null>(null)
  const [responseData, setResponseData] = useState<unknown>(null)
  const [errorMessage, setErrorMessage] = useState<string | null>(null)

  // Preview & History
  const [requestPreviewTab, setRequestPreviewTab] = useState<number>(0)
  const [copied, setCopied] = useState<boolean>(false)
  const [callHistory, setCallHistory] = useState<CallHistoryItem[]>([])

  // Lookup data for FKs
  const [customers, setCustomers] = useState<Customer[]>([])
  const [users, setUsers] = useState<UserSummary[]>([])

  // Selected endpoint definition
  const currentEndpoint: EndpointDefinition = useMemo(() => {
    return ALL_ENDPOINTS.find((ep) => ep.id === selectedEndpointId) ?? ALL_ENDPOINTS[0]
  }, [selectedEndpointId])

  // Filtered endpoints based on selected category
  const filteredEndpoints: EndpointDefinition[] = useMemo(() => {
    if (selectedCategory === 'all') return ALL_ENDPOINTS
    return ALL_ENDPOINTS.filter((ep) => ep.category === selectedCategory)
  }, [selectedCategory])

  // Load lookup options
  useEffect(() => {
    const rawSession = localStorage.getItem('voltflow.session')
    if (!rawSession) return

    let active = true
    Promise.all([
      customersApi.list().catch(() => []),
      usersApi.page({ limit: 50 }).catch(() => ({ items: [] })),
    ]).then(([custList, userList]) => {
      if (!active) return
      setCustomers(custList)
      setUsers(userList.items ?? [])
    })
    return () => {
      active = false
    }
  }, [])

  // Auto-fill mock values for current endpoint
  const populateMock = useCallback((ep: EndpointDefinition) => {
    const lookups = {
      customerId: customers[0]?.id,
      userId: users[0]?.id,
    }
    const mock = ep.generateMock ? ep.generateMock(lookups) : {}
    setPathValues(mock.path ?? {})
    setQueryValues(mock.query ?? {})
    setBodyValues(mock.body ?? {})
    setErrorMessage(null)
  }, [customers, users])

  // Reset form when endpoint changes
  useEffect(() => {
    populateMock(currentEndpoint)
  }, [currentEndpoint, populateMock])

  // Category switch
  const handleCategoryChange = (catId: string) => {
    setSelectedCategory(catId)
    if (catId !== 'all') {
      const match = ALL_ENDPOINTS.find((ep) => ep.category === catId)
      if (match && match.id !== selectedEndpointId) {
        setSelectedEndpointId(match.id)
      }
    }
  }

  // Clear form inputs
  const handleClear = () => {
    const emptyBody: Record<string, unknown> = {}
    currentEndpoint.bodyFields?.forEach((f) => {
      if (f.type === 'boolean') emptyBody[f.name] = false
      else if (f.type === 'number') emptyBody[f.name] = 0
      else emptyBody[f.name] = ''
    })
    setPathValues({})
    setQueryValues({})
    setBodyValues(emptyBody)
    setErrorMessage(null)
  }

  // Construct target URL with path and query parameters
  const computedUrl = useMemo(() => {
    let rawPath = currentEndpoint.path
    // Replace {param} with pathValues[param]
    Object.entries(pathValues).forEach(([paramKey, val]) => {
      rawPath = rawPath.replace(`{${paramKey}}`, encodeURIComponent(val))
    })

    // Construct query parameters
    const searchParams = new URLSearchParams()
    Object.entries(queryValues).forEach(([k, v]) => {
      if (v !== undefined && v !== null && v !== '') {
        searchParams.append(k, String(v))
      }
    })

    const queryString = searchParams.toString()
    return queryString ? `${rawPath}?${queryString}` : rawPath
  }, [currentEndpoint.path, pathValues, queryValues])

  // Generate cURL command preview
  const generatedCurl = useMemo(() => {
    const sessionStr = typeof window !== 'undefined' ? localStorage.getItem('voltflow.session') : null
    const token = sessionStr ? (JSON.parse(sessionStr) as AuthResult).token : '<TOKEN>'

    const parts: string[] = [
      `curl -X ${currentEndpoint.method} "http://localhost:5275${computedUrl}"`,
      '  -H "Content-Type: application/json"',
      `  -H "Authorization: Bearer ${token}"`,
    ]

    if (currentEndpoint.method !== 'GET') {
      parts.push(`  -H "Idempotency-Key: ${crypto.randomUUID()}"`)
    }

    if (currentEndpoint.method !== 'GET' && Object.keys(bodyValues).length > 0) {
      parts.push(`  -d '${JSON.stringify(bodyValues, null, 2).replace(/'/g, "\\'")}'`)
    }

    return parts.join(' \\\n')
  }, [currentEndpoint.method, computedUrl, bodyValues])

  // Copy to clipboard
  const handleCopy = () => {
    const text = requestPreviewTab === 0 ? generatedCurl : JSON.stringify(bodyValues, null, 2)
    navigator.clipboard.writeText(text)
    setCopied(true)
    setTimeout(() => setCopied(false), 2000)
  }

  // Execute endpoint call
  const handleExecute = async () => {
    setExecuting(true)
    setErrorMessage(null)
    const startTime = performance.now()

    try {
      const sessionStr = typeof window !== 'undefined' ? localStorage.getItem('voltflow.session') : null
      const token = sessionStr ? (JSON.parse(sessionStr) as AuthResult).token : undefined

      const init: RequestInit = {
        method: currentEndpoint.method,
        headers: {
          'Content-Type': 'application/json',
          ...(token ? { Authorization: `Bearer ${token}` } : {}),
          'X-Client-Screen': '/endpoint-trigger',
          'X-Client-Action': `${currentEndpoint.method} ${currentEndpoint.path}`,
          ...(currentEndpoint.method !== 'GET' ? { 'Idempotency-Key': crypto.randomUUID() } : {}),
        },
      }

      if (currentEndpoint.method !== 'GET' && Object.keys(bodyValues).length > 0) {
        init.body = JSON.stringify(bodyValues)
      }

      const response = await fetch(computedUrl, init)
      const duration = Math.round(performance.now() - startTime)

      // Parse body
      let data: unknown
      const contentType = response.headers.get('content-type')
      if (contentType && contentType.includes('application/json')) {
        data = await response.json().catch(() => null)
      } else {
        data = await response.text().catch(() => '')
      }

      setResponseStatus({
        code: response.status,
        text: response.statusText,
        success: response.ok,
        duration,
      })
      setResponseData(data)

      // Add to history
      setCallHistory((prev) => [
        {
          id: crypto.randomUUID(),
          method: currentEndpoint.method,
          url: computedUrl,
          status: response.status,
          statusText: response.statusText,
          durationMs: duration,
          timestamp: new Date().toLocaleTimeString('tr-TR'),
          success: response.ok,
        },
        ...prev.slice(0, 9),
      ])
    } catch (err: unknown) {
      const duration = Math.round(performance.now() - startTime)
      const msg = err instanceof Error ? err.message : 'Ağ veya bağlantı hatası oluştu.'
      setErrorMessage(msg)
      setResponseStatus({
        code: 0,
        text: 'Connection Error',
        success: false,
        duration,
      })
      setResponseData({ error: msg })
    } finally {
      setExecuting(false)
    }
  }

  // Method color helper
  const getMethodColor = (m: HttpMethod): 'success' | 'info' | 'warning' | 'error' | 'default' => {
    switch (m) {
      case 'POST':
        return 'success'
      case 'GET':
        return 'info'
      case 'PUT':
        return 'warning'
      case 'DELETE':
        return 'error'
      default:
        return 'default'
    }
  }

  return (
    <Box sx={{ p: { xs: 2, md: 3 }, maxWidth: 1440, mx: 'auto' }}>
      {/* Top Header */}
      <Box sx={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', flexWrap: 'wrap', gap: 2, mb: 3 }}>
        <Box>
          <Box sx={{ display: 'flex', alignItems: 'center', gap: 1.5 }}>
            <HttpIcon color="primary" sx={{ fontSize: 34 }} />
            <Typography variant="h4" sx={{ fontWeight: 700 }}>
              {'Endpoint Tetikleyici (API Runner)'}
            </Typography>
          </Box>
          <Typography variant="body2" color="text.secondary" sx={{ mt: 0.5 }}>
            {'Sistemdeki tüm REST API endpoint\'lerini dinamik formlar, enum dropdown\'ları ve anlık yanıt inceleyici ile doğrudan tetikleyin.'}
          </Typography>
        </Box>

        <Stack direction="row" spacing={1.5} sx={{ flexWrap: 'wrap' }}>
          <Button
            variant="outlined"
            color="primary"
            startIcon={<AutoFixHighIcon />}
            onClick={() => populateMock(currentEndpoint)}
            data-testid="btn-randomize-endpoint"
          >
            {t('common:generator.randomize')}
          </Button>
          <Button
            variant="outlined"
            color="secondary"
            startIcon={<ClearIcon />}
            onClick={handleClear}
            data-testid="btn-clear-endpoint-form"
          >
            {t('common:generator.clear')}
          </Button>
          <Button
            variant="contained"
            color="primary"
            startIcon={executing ? <CircularProgress size={18} color="inherit" /> : <PlayArrowIcon />}
            onClick={handleExecute}
            disabled={executing}
            data-testid="btn-execute-endpoint"
          >
            {executing ? 'İstek Gönderiliyor...' : 'Endpoint\'i Tetikle'}
          </Button>
        </Stack>
      </Box>

      {/* Error Alert */}
      {errorMessage && (
        <Alert severity="error" sx={{ mb: 3 }} onClose={() => setErrorMessage(null)}>
          {errorMessage}
        </Alert>
      )}

      {/* Category Filter Chips */}
      <Paper sx={{ p: 1.5, mb: 3 }} variant="outlined">
        <Typography variant="caption" color="text.secondary" sx={{ display: 'block', mb: 1, fontWeight: 600 }}>
          {'Modül & Kategori Filtresi'}
        </Typography>
        <Stack direction="row" spacing={1} sx={{ overflowX: 'auto', pb: 0.5 }}>
          {ENDPOINT_CATEGORIES.map((cat) => {
            const isSelected = selectedCategory === cat.id
            return (
              <Chip
                key={cat.id}
                label={cat.label}
                clickable
                color={isSelected ? 'primary' : 'default'}
                variant={isSelected ? 'filled' : 'outlined'}
                onClick={() => handleCategoryChange(cat.id)}
                data-testid={`category-chip-${cat.id}`}
                sx={{ fontWeight: isSelected ? 600 : 400, flexShrink: 0 }}
              />
            )
          })}
        </Stack>
      </Paper>

      {/* Endpoint Selector Dropdown & Info Card */}
      <Card variant="outlined" sx={{ mb: 3 }}>
        <CardContent sx={{ pb: 2 }}>
          <Box sx={{ display: 'grid', gridTemplateColumns: { xs: '1fr', md: '8fr 4fr' }, gap: 2, alignItems: 'center' }}>
            <TextField
              select
              fullWidth
              label={'Tetiklenecek Endpoint\'i Seçin'}
              value={currentEndpoint.id}
              onChange={(e) => setSelectedEndpointId(e.target.value)}
              data-testid="select-endpoint-dropdown"
              helperText={currentEndpoint.description}
            >
              {filteredEndpoints.map((ep) => (
                <MenuItem key={ep.id} value={ep.id}>
                  <Box sx={{ display: 'flex', alignItems: 'center', gap: 1.5, width: '100%' }}>
                    <Chip
                      label={ep.method}
                      color={getMethodColor(ep.method)}
                      size="small"
                      sx={{ fontWeight: 700, fontSize: '0.72rem', minWidth: 54 }}
                    />
                    <Typography variant="body2" sx={{ fontWeight: 600 }}>
                      {ep.title}
                    </Typography>
                    <Typography variant="caption" color="text.secondary" sx={{ ml: 'auto', fontFamily: 'monospace' }}>
                      {ep.path}
                    </Typography>
                  </Box>
                </MenuItem>
              ))}
            </TextField>

            <Box sx={{ display: 'flex', alignItems: 'center', justifyContent: { xs: 'flex-start', md: 'flex-end' }, gap: 1, flexWrap: 'wrap' }}>
              <Chip label={currentEndpoint.method} color={getMethodColor(currentEndpoint.method)} sx={{ fontWeight: 700 }} />
              <Paper variant="outlined" sx={{ px: 1.5, py: 0.5, fontFamily: 'monospace', fontSize: '0.85rem' }}>
                {computedUrl}
              </Paper>
            </Box>
          </Box>
        </CardContent>
      </Card>

      {/* Main Grid: Form Left (7 cols) + Studio/Inspector Right (5 cols) */}
      <Box sx={{ display: 'grid', gridTemplateColumns: { xs: '1fr', lg: '7fr 5fr' }, gap: 3 }}>
        {/* Left Form Area */}
        <Paper sx={{ p: 3 }} variant="outlined">
          <Box sx={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', mb: 2 }}>
            <Box>
              <Typography variant="h6" sx={{ fontWeight: 600 }}>
                {currentEndpoint.title}
              </Typography>
              <Typography variant="caption" color="text.secondary">
                {currentEndpoint.method} {currentEndpoint.path}
              </Typography>
            </Box>
            <Chip label={currentEndpoint.category.toUpperCase()} size="small" variant="outlined" color="primary" />
          </Box>

          <Divider sx={{ mb: 2.5 }} />

          <Stack spacing={2.5}>
            {/* 1. PATH PARAMETERS (If any) */}
            {currentEndpoint.pathParams && currentEndpoint.pathParams.length > 0 && (
              <Box>
                <Typography variant="subtitle2" sx={{ fontWeight: 600, mb: 1.5, color: 'primary.main' }}>
                  {'Path Parametreleri (URL Yol Parametreleri)'}
                </Typography>
                <Box sx={{ display: 'grid', gridTemplateColumns: { xs: '1fr', sm: '1fr 1fr' }, gap: 2 }}>
                  {currentEndpoint.pathParams.map((param) => (
                    <TextField
                      key={param.name}
                      fullWidth
                      label={`${param.label} ({${param.name}})`}
                      value={pathValues[param.name] ?? ''}
                      onChange={(e) => setPathValues({ ...pathValues, [param.name]: e.target.value })}
                      required={param.required}
                      helperText={param.helperText || 'Örn: GUID veya ID'}
                      data-testid={`path-param-${param.name}`}
                    />
                  ))}
                </Box>
              </Box>
            )}

            {/* 2. QUERY PARAMETERS (If any) */}
            {currentEndpoint.queryParams && currentEndpoint.queryParams.length > 0 && (
              <Box>
                <Typography variant="subtitle2" sx={{ fontWeight: 600, mb: 1.5, color: 'primary.main' }}>
                  {'Sorgu Parametreleri (Query Parameters)'}
                </Typography>
                <Box sx={{ display: 'grid', gridTemplateColumns: { xs: '1fr', sm: '1fr 1fr' }, gap: 2 }}>
                  {currentEndpoint.queryParams.map((param) => {
                    if (param.type === 'enum' && param.options) {
                      return (
                        <TextField
                          key={param.name}
                          select
                          fullWidth
                          label={param.label}
                          value={queryValues[param.name] ?? ''}
                          onChange={(e) => setQueryValues({ ...queryValues, [param.name]: e.target.value })}
                          data-testid={`query-param-${param.name}`}
                        >
                          {param.options.map((opt) => (
                            <MenuItem key={String(opt.value)} value={opt.value as string}>
                              <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                                <Chip label={opt.label} color={opt.color || 'default'} size="small" />
                              </Box>
                            </MenuItem>
                          ))}
                        </TextField>
                      )
                    }

                    return (
                      <TextField
                        key={param.name}
                        fullWidth
                        type={param.type === 'number' ? 'number' : 'text'}
                        label={param.label}
                        value={queryValues[param.name] ?? ''}
                        onChange={(e) => setQueryValues({ ...queryValues, [param.name]: e.target.value })}
                        helperText={param.helperText}
                        data-testid={`query-param-${param.name}`}
                      />
                    )
                  })}
                </Box>
              </Box>
            )}

            {/* 3. REQUEST BODY (If any) */}
            {currentEndpoint.bodyFields && currentEndpoint.bodyFields.length > 0 && (
              <Box>
                <Typography variant="subtitle2" sx={{ fontWeight: 600, mb: 1.5, color: 'primary.main' }}>
                  {'İstek Gövdesi (Request Body Payload)'}
                </Typography>
                <Box sx={{ display: 'grid', gridTemplateColumns: { xs: '1fr', sm: '1fr 1fr' }, gap: 2.5 }}>
                  {currentEndpoint.bodyFields.map((field: EndpointField) => {
                    const isLongText =
                      field.name.toLowerCase().includes('description') ||
                      field.name.toLowerCase().includes('notes') ||
                      field.name.toLowerCase().includes('reason') ||
                      field.name.toLowerCase().includes('signature') ||
                      field.name.toLowerCase().includes('photo')

                    // A. BOOLEAN (SWITCH)
                    if (field.type === 'boolean') {
                      return (
                        <Box key={field.name} sx={{ gridColumn: { xs: '1', sm: 'span 1' } }}>
                          <Paper variant="outlined" sx={{ px: 2, py: 1, height: '100%', display: 'flex', alignItems: 'center' }}>
                            <FormControlLabel
                              control={
                                <Switch
                                  checked={Boolean(bodyValues[field.name])}
                                  onChange={(e) => setBodyValues({ ...bodyValues, [field.name]: e.target.checked })}
                                  data-testid={`body-switch-${field.name}`}
                                />
                              }
                              label={field.label}
                            />
                          </Paper>
                        </Box>
                      )
                    }

                    // B. ENUM (DROPDOWN)
                    if (field.type === 'enum' && field.options) {
                      return (
                        <Box key={field.name} sx={{ gridColumn: { xs: '1', sm: 'span 1' } }}>
                          <TextField
                            select
                            fullWidth
                            label={field.label}
                            value={bodyValues[field.name] ?? ''}
                            onChange={(e) => setBodyValues({ ...bodyValues, [field.name]: e.target.value })}
                            helperText={field.helperText}
                            required={field.required}
                            data-testid={`body-select-${field.name}`}
                          >
                            {field.options.map((opt) => (
                              <MenuItem key={String(opt.value)} value={opt.value as string}>
                                <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                                  <Chip label={opt.label} color={opt.color || 'default'} size="small" />
                                </Box>
                              </MenuItem>
                            ))}
                          </TextField>
                        </Box>
                      )
                    }

                    // C. FOREIGN KEY (CUSTOMER / USER SHORTCUT)
                    if (field.type === 'fk') {
                      const isCustomerFk = field.fkEntity === 'Customer' && customers.length > 0
                      const isUserFk = field.fkEntity === 'User' && users.length > 0

                      return (
                        <Box key={field.name} sx={{ gridColumn: { xs: '1', sm: 'span 2' } }}>
                          {isCustomerFk ? (
                            <TextField
                              select
                              fullWidth
                              label={field.label}
                              value={bodyValues[field.name] || ''}
                              onChange={(e) => setBodyValues({ ...bodyValues, [field.name]: e.target.value })}
                              helperText={field.helperText || 'Mevcut müşterilerden birini seçin'}
                              required={field.required}
                              data-testid={`body-fk-${field.name}`}
                            >
                              {customers.map((c) => (
                                <MenuItem key={c.id} value={c.id}>
                                  <Box sx={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', width: '100%' }}>
                                    <Typography variant="body2">{c.fullName}</Typography>
                                    <Typography variant="caption" color="text.secondary">
                                      ({c.email})
                                    </Typography>
                                  </Box>
                                </MenuItem>
                              ))}
                            </TextField>
                          ) : isUserFk ? (
                            <TextField
                              select
                              fullWidth
                              label={field.label}
                              value={bodyValues[field.name] || ''}
                              onChange={(e) => setBodyValues({ ...bodyValues, [field.name]: e.target.value })}
                              helperText={field.helperText || 'Sistem kullanıcılarından birini seçin'}
                              required={field.required}
                              data-testid={`body-fk-${field.name}`}
                            >
                              {users.map((u) => (
                                <MenuItem key={u.id} value={u.id}>
                                  <Box sx={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', width: '100%' }}>
                                    <Typography variant="body2">{u.name}</Typography>
                                    <Typography variant="caption" color="text.secondary">
                                      ({u.email})
                                    </Typography>
                                  </Box>
                                </MenuItem>
                              ))}
                            </TextField>
                          ) : (
                            <TextField
                              fullWidth
                              label={field.label}
                              value={bodyValues[field.name] ?? ''}
                              onChange={(e) => setBodyValues({ ...bodyValues, [field.name]: e.target.value })}
                              helperText={field.helperText || 'UUID / GUID kimliği'}
                              required={field.required}
                              data-testid={`body-input-${field.name}`}
                            />
                          )}
                        </Box>
                      )
                    }

                    // D. NUMBER
                    if (field.type === 'number') {
                      return (
                        <Box key={field.name} sx={{ gridColumn: { xs: '1', sm: 'span 1' } }}>
                          <TextField
                            fullWidth
                            type="number"
                            label={field.label}
                            value={bodyValues[field.name] ?? ''}
                            onChange={(e) =>
                              setBodyValues({
                                ...bodyValues,
                                [field.name]: e.target.value === '' ? '' : Number(e.target.value),
                              })
                            }
                            helperText={field.helperText}
                            required={field.required}
                            data-testid={`body-input-${field.name}`}
                          />
                        </Box>
                      )
                    }

                    // E. DATE
                    if (field.type === 'date') {
                      return (
                        <Box key={field.name} sx={{ gridColumn: { xs: '1', sm: 'span 1' } }}>
                          <TextField
                            fullWidth
                            type="date"
                            label={field.label}
                            slotProps={{ inputLabel: { shrink: true } }}
                            value={bodyValues[field.name] ?? ''}
                            onChange={(e) => setBodyValues({ ...bodyValues, [field.name]: e.target.value })}
                            helperText={field.helperText}
                            required={field.required}
                            data-testid={`body-input-${field.name}`}
                          />
                        </Box>
                      )
                    }

                    // F. TEXT
                    return (
                      <Box key={field.name} sx={{ gridColumn: { xs: '1', sm: isLongText ? 'span 2' : 'span 1' } }}>
                        <TextField
                          fullWidth
                          multiline={isLongText}
                          rows={isLongText ? 2 : 1}
                          label={field.label}
                          value={bodyValues[field.name] ?? ''}
                          onChange={(e) => setBodyValues({ ...bodyValues, [field.name]: e.target.value })}
                          helperText={field.helperText}
                          required={field.required}
                          data-testid={`body-input-${field.name}`}
                        />
                      </Box>
                    )
                  })}
                </Box>
              </Box>
            )}

            {/* GET Request without body notice */}
            {currentEndpoint.method === 'GET' && (!currentEndpoint.queryParams || currentEndpoint.queryParams.length === 0) && (
              <Alert severity="info">
                {'Bu GET endpoint\'i gövde (body) parametresi almaz. Doğrudan yukarıdaki "Endpoint\'i Tetikle" butonuna basarak yanıtı test edebilirsiniz.'}
              </Alert>
            )}
          </Stack>
        </Paper>

        {/* Right Area: Request Preview + Live HTTP Response Inspector */}
        <Stack spacing={3}>
          {/* 1. Request Preview Tabs */}
          <Card variant="outlined">
            <CardContent>
              <Box sx={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', mb: 1.5 }}>
                <Tabs
                  value={requestPreviewTab}
                  onChange={(_, val: number) => setRequestPreviewTab(val)}
                  textColor="primary"
                  indicatorColor="primary"
                  sx={{ minHeight: 36 }}
                >
                  <Tab
                    icon={<TerminalIcon fontSize="small" />}
                    iconPosition="start"
                    label={'cURL Komutu'}
                    sx={{ minHeight: 36, textTransform: 'none', py: 0.5 }}
                    data-testid="tab-curl-preview"
                  />
                  <Tab
                    icon={<DataObjectIcon fontSize="small" />}
                    iconPosition="start"
                    label={'Request JSON'}
                    sx={{ minHeight: 36, textTransform: 'none', py: 0.5 }}
                    data-testid="tab-req-json"
                  />
                </Tabs>

                <Tooltip title={copied ? t('common:generator.copySuccess') : 'Kopyala'}>
                  <IconButton size="small" onClick={handleCopy} data-testid="btn-copy-req-preview">
                    {copied ? <CheckCircleIcon color="success" fontSize="small" /> : <ContentCopyIcon fontSize="small" />}
                  </IconButton>
                </Tooltip>
              </Box>

              <Paper
                elevation={0}
                sx={{
                  p: 1.5,
                  bgcolor: 'action.hover',
                  borderRadius: 1,
                  fontFamily: 'monospace',
                  fontSize: '0.78rem',
                  overflowX: 'auto',
                  maxHeight: 220,
                  whiteSpace: 'pre-wrap',
                  wordBreak: 'break-all',
                }}
              >
                {requestPreviewTab === 0 ? generatedCurl : JSON.stringify(bodyValues, null, 2)}
              </Paper>
            </CardContent>
          </Card>

          {/* 2. Live HTTP Response Inspector */}
          <Card variant="outlined">
            <CardContent>
              <Box sx={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', mb: 1.5 }}>
                <Typography variant="subtitle2" sx={{ fontWeight: 600 }}>
                  {'HTTP Yanıt İnceleyici (Response Inspector)'}
                </Typography>
                {responseStatus && (
                  <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                    <Chip
                      icon={<TimerIcon fontSize="small" />}
                      label={`${responseStatus.duration} ms`}
                      size="small"
                      variant="outlined"
                      sx={{ fontSize: '0.72rem' }}
                    />
                    <Chip
                      label={`${responseStatus.code} ${responseStatus.text}`}
                      color={responseStatus.success ? 'success' : 'error'}
                      size="small"
                      sx={{ fontWeight: 700 }}
                      data-testid="chip-response-status"
                    />
                  </Box>
                )}
              </Box>

              <Paper
                elevation={0}
                sx={{
                  p: 1.5,
                  bgcolor: 'action.hover',
                  borderRadius: 1,
                  fontFamily: 'monospace',
                  fontSize: '0.78rem',
                  overflowX: 'auto',
                  maxHeight: 280,
                  whiteSpace: 'pre-wrap',
                  wordBreak: 'break-all',
                }}
                data-testid="response-json-box"
              >
                {responseData !== null ? (
                  <pre style={{ margin: 0 }}>{JSON.stringify(responseData, null, 2)}</pre>
                ) : (
                  <Typography variant="caption" color="text.secondary">
                    {'Henüz istek gönderilmedi. Test etmek için yukarıdaki "Endpoint\'i Tetikle" butonuna tıklayınız.'}
                  </Typography>
                )}
              </Paper>
            </CardContent>
          </Card>

          {/* 3. Session Call History */}
          <Card variant="outlined">
            <CardContent>
              <Typography variant="subtitle2" sx={{ fontWeight: 600, mb: 1.5 }}>
                {'Bu Oturumda Tetiklenen İstekler'} ({callHistory.length})
              </Typography>
              {callHistory.length === 0 ? (
                <Typography variant="caption" color="text.secondary">
                  {'Henüz endpoint çağrısı yapılmadı.'}
                </Typography>
              ) : (
                <Stack spacing={1}>
                  {callHistory.map((item) => (
                    <Paper
                      key={item.id}
                      variant="outlined"
                      sx={{ p: 1, display: 'flex', alignItems: 'center', justifyContent: 'space-between', gap: 1 }}
                    >
                      <Box sx={{ display: 'flex', alignItems: 'center', gap: 1, overflow: 'hidden' }}>
                        <Chip
                          label={item.method}
                          color={getMethodColor(item.method)}
                          size="small"
                          sx={{ fontSize: '0.68rem', height: 20, minWidth: 46, fontWeight: 700 }}
                        />
                        <Typography variant="body2" noWrap sx={{ fontSize: '0.8rem', fontFamily: 'monospace' }}>
                          {item.url}
                        </Typography>
                      </Box>
                      <Box sx={{ display: 'flex', alignItems: 'center', gap: 1, flexShrink: 0 }}>
                        <Chip
                          label={item.status}
                          color={item.success ? 'success' : 'error'}
                          size="small"
                          sx={{ fontSize: '0.7rem', height: 20 }}
                        />
                        <Typography variant="caption" color="text.secondary" sx={{ fontSize: '0.72rem' }}>
                          {`${item.durationMs} ms`}
                        </Typography>
                      </Box>
                    </Paper>
                  ))}
                </Stack>
              )}
            </CardContent>
          </Card>
        </Stack>
      </Box>
    </Box>
  )
}

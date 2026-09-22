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
import AutoFixHighIcon from '@mui/icons-material/AutoFixHigh'
import SaveIcon from '@mui/icons-material/Save'
import ContentCopyIcon from '@mui/icons-material/ContentCopy'
import CheckCircleIcon from '@mui/icons-material/CheckCircle'
import StorageIcon from '@mui/icons-material/Storage'
import ClearIcon from '@mui/icons-material/Clear'
import CodeIcon from '@mui/icons-material/Code'
import DataObjectIcon from '@mui/icons-material/DataObject'
import RefreshIcon from '@mui/icons-material/Refresh'

import {
  testDataApi,
  customersApi,
  usersApi,
  type Customer,
  type UserSummary,
} from '../api'
import {
  ALL_TABLES,
  TABLE_CATEGORIES,
  type TableDefinition,
  type FieldDefinition,
} from './schemaRegistry'
import { useI18n } from '../i18n'

interface CreatedRecord {
  id: string
  table: string
  title: string
  timestamp: string
}

export function TestDataGeneratorView() {
  const { translate: t } = useI18n()

  // State
  const [selectedCategory, setSelectedCategory] = useState<string>('all')
  const [selectedTableName, setSelectedTableName] = useState<string>('AppUsers')
  const [formData, setFormData] = useState<Record<string, unknown>>({})
  const [tableCounts, setTableCounts] = useState<Record<string, number>>({})
  const [loadingCounts, setLoadingCounts] = useState<boolean>(false)

  const [previewTab, setPreviewTab] = useState<number>(0)
  const [copied, setCopied] = useState<boolean>(false)

  const [submitting, setSubmitting] = useState<boolean>(false)
  const [feedback, setFeedback] = useState<{ type: 'success' | 'error'; message: string; recordId?: string } | null>(null)
  const [history, setHistory] = useState<CreatedRecord[]>([])

  // Lookup references for FKs
  const [customers, setCustomers] = useState<Customer[]>([])
  const [users, setUsers] = useState<UserSummary[]>([])

  // Current active table definition
  const currentTable: TableDefinition = useMemo(() => {
    return ALL_TABLES.find((table) => table.name === selectedTableName) ?? ALL_TABLES[0]
  }, [selectedTableName])

  // Filtered tables list based on selected category
  const filteredTables: TableDefinition[] = useMemo(() => {
    if (selectedCategory === 'all') return ALL_TABLES
    return ALL_TABLES.filter((table) => table.category === selectedCategory)
  }, [selectedCategory])

  // Fetch live table row counts
  const fetchCounts = useCallback(async () => {
    setLoadingCounts(true)
    try {
      const counts = await testDataApi.getCounts()
      setTableCounts(counts)
    } catch {
      // Non-blocking fallback
    } finally {
      setLoadingCounts(false)
    }
  }, [])

  // Initial load
  useEffect(() => {
    let active = true
    fetchCounts()

    const rawSession = localStorage.getItem('voltflow.session')
    if (rawSession) {
      Promise.all([
        customersApi.list().catch(() => []),
        usersApi.page({ limit: 50 }).catch(() => ({ items: [] })),
      ]).then(([custList, userList]) => {
        if (!active) return
        setCustomers(custList)
        setUsers(userList.items ?? [])
      })
    }

    return () => {
      active = false
    }
  }, [fetchCounts])

  // Populate mock data for the current table
  const populateMock = useCallback((table: TableDefinition) => {
    const lookups = {
      customerId: customers[0]?.id,
      userId: users[0]?.id,
    }
    const mock = table.generateMock(lookups)
    setFormData(mock)
    setFeedback(null)
  }, [customers, users])

  // When table changes, reset form with mock data
  useEffect(() => {
    populateMock(currentTable)
  }, [currentTable, populateMock])

  // Category switch handler
  const handleCategoryChange = (category: string) => {
    setSelectedCategory(category)
    if (category !== 'all') {
      const match = ALL_TABLES.find((tbl) => tbl.category === category)
      if (match && match.name !== selectedTableName) {
        setSelectedTableName(match.name)
      }
    }
  }

  // Randomize button
  const handleRandomize = () => {
    populateMock(currentTable)
  }

  // Clear form button
  const handleClear = () => {
    const empty: Record<string, unknown> = {}
    currentTable.fields.forEach((f) => {
      if (f.type === 'boolean') empty[f.name] = false
      else if (f.type === 'number') empty[f.name] = 0
      else empty[f.name] = ''
    })
    setFormData(empty)
    setFeedback(null)
  }

  // Live SQL Generator
  const generatedSql = useMemo(() => {
    const keys = Object.keys(formData).filter(
      (key) => formData[key] !== undefined && formData[key] !== null && formData[key] !== ''
    )

    if (keys.length === 0) {
      return `-- ${currentTable.name} tablosu için form alanlarını doldurunuz.`
    }

    const quotedColumns = keys.map((key) => `"${key}"`).join(', ')
    const values = keys
      .map((key) => {
        const val = formData[key]
        if (typeof val === 'number') return String(val)
        if (typeof val === 'boolean') return val ? 'TRUE' : 'FALSE'
        return `'${String(val).replace(/'/g, "''")}'`
      })
      .join(', ')

    return `INSERT INTO "${currentTable.name}" (${quotedColumns})\nVALUES (${values});`
  }, [currentTable.name, formData])

  // Live JSON string
  const jsonPreview = useMemo(() => {
    return JSON.stringify(formData, null, 2)
  }, [formData])

  // Copy to clipboard handler
  const handleCopy = () => {
    const text = previewTab === 0 ? generatedSql : jsonPreview
    navigator.clipboard.writeText(text)
    setCopied(true)
    setTimeout(() => setCopied(false), 2000)
  }

  // Direct Save to DB handler
  const handleSave = async () => {
    setSubmitting(true)
    setFeedback(null)

    try {
      const res = await testDataApi.insert(currentTable.name, formData)
      if (res.success) {
        const recordId = res.id ?? 'yeni-kayit'
        setFeedback({
          type: 'success',
          message: `${currentTable.displayName} tablosuna yeni test verisi başarıyla eklendi!`,
          recordId,
        })

        // Refresh row counts
        fetchCounts()

        // History entry title
        const titleField =
          (formData.Name as string) ||
          (formData.Title as string) ||
          (formData.Number as string) ||
          (formData.FullName as string) ||
          (formData.Description as string) ||
          (formData.Code as string) ||
          recordId

        setHistory((prev) => [
          {
            id: recordId,
            table: currentTable.name,
            title: String(titleField),
            timestamp: new Date().toLocaleTimeString('tr-TR'),
          },
          ...prev.slice(0, 9),
        ])
      } else {
        setFeedback({
          type: 'error',
          message: res.message || 'Kayıt eklenirken bir hata oluştu.',
        })
      }
    } catch (err: unknown) {
      const msg = err instanceof Error ? err.message : 'Kayıt oluşturulurken bir hata oluştu.'
      setFeedback({ type: 'error', message: msg })
    } finally {
      setSubmitting(false)
    }
  }

  // Field change helper
  const handleFieldChange = (name: string, value: unknown) => {
    setFormData((prev) => ({ ...prev, [name]: value }))
  }

  return (
    <Box sx={{ p: { xs: 2, md: 3 }, maxWidth: 1440, mx: 'auto' }}>
      {/* Top Header */}
      <Box sx={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', flexWrap: 'wrap', gap: 2, mb: 3 }}>
        <Box>
          <Box sx={{ display: 'flex', alignItems: 'center', gap: 1.5 }}>
            <AutoFixHighIcon color="primary" sx={{ fontSize: 34 }} />
            <Typography variant="h4" sx={{ fontWeight: 700 }}>
              {t('common:generator.title')}
            </Typography>
          </Box>
          <Typography variant="body2" color="text.secondary" sx={{ mt: 0.5 }}>
            {t('common:generator.subtitle')}
          </Typography>
        </Box>

        <Stack direction="row" spacing={1.5} sx={{ flexWrap: 'wrap' }}>
          <Button
            variant="outlined"
            color="primary"
            startIcon={<AutoFixHighIcon />}
            onClick={handleRandomize}
            data-testid="btn-randomize-data"
          >
            {t('common:generator.randomize')}
          </Button>
          <Button
            variant="outlined"
            color="secondary"
            startIcon={<ClearIcon />}
            onClick={handleClear}
            data-testid="btn-clear-form"
          >
            {t('common:generator.clear')}
          </Button>
          <Button
            variant="contained"
            color="primary"
            startIcon={submitting ? <CircularProgress size={18} color="inherit" /> : <SaveIcon />}
            onClick={handleSave}
            disabled={submitting}
            data-testid="btn-save-test-data"
          >
            {submitting ? t('common:generator.saving') : t('common:generator.save')}
          </Button>
        </Stack>
      </Box>

      {/* Feedback Alert */}
      {feedback && (
        <Alert
          severity={feedback.type}
          sx={{ mb: 3 }}
          onClose={() => setFeedback(null)}
          data-testid="feedback-alert"
        >
          <strong>{feedback.message}</strong>
          {feedback.recordId && (
            <Typography variant="caption" sx={{ display: 'block', mt: 0.5 }}>
              {t('common:generator.createdId')} <code>{feedback.recordId}</code>
            </Typography>
          )}
        </Alert>
      )}

      {/* Category Filter Chips */}
      <Paper sx={{ p: 1.5, mb: 3 }} variant="outlined">
        <Typography variant="caption" color="text.secondary" sx={{ display: 'block', mb: 1, fontWeight: 600 }}>
          {t('common:generator.selectCategory')}
        </Typography>
        <Stack direction="row" spacing={1} sx={{ overflowX: 'auto', pb: 0.5 }}>
          {TABLE_CATEGORIES.map((cat) => {
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

      {/* Table Selector & Table Info Bar */}
      <Card variant="outlined" sx={{ mb: 3 }}>
        <CardContent sx={{ pb: 2 }}>
          <Box sx={{ display: 'grid', gridTemplateColumns: { xs: '1fr', md: '7fr 5fr' }, gap: 2, alignItems: 'center' }}>
            <TextField
              select
              fullWidth
              label={t('common:generator.selectTable')}
              value={currentTable.name}
              onChange={(e) => {
                setSelectedTableName(e.target.value)
                setFeedback(null)
              }}
              data-testid="select-table-dropdown"
              helperText={currentTable.description}
            >
              {filteredTables.map((tbl) => {
                const count = tableCounts[tbl.name]
                return (
                  <MenuItem key={tbl.name} value={tbl.name} data-testid="menuitem-b0be24">
                    <Box sx={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', width: '100%', gap: 1 }}>
                      <Typography variant="body2" sx={{ fontWeight: 500 }}>
                        {tbl.displayName}
                      </Typography>
                      <Chip
                        size="small"
                        label={count !== undefined ? `${count} ${t('common:generator.rows')}` : '...'}
                        color={count !== undefined && count > 0 ? 'success' : 'default'}
                        variant="outlined"
                        sx={{ fontSize: '0.72rem', height: 20 }}
                      />
                    </Box>
                  </MenuItem>
                )
              })}
            </TextField>

            <Box sx={{ display: 'flex', alignItems: 'center', justifyContent: { xs: 'flex-start', md: 'flex-end' }, gap: 1.5, flexWrap: 'wrap' }}>
              <Chip
                icon={<StorageIcon fontSize="small" />}
                label={`${t('common:generator.rowCount')}: ${tableCounts[currentTable.name] ?? 0}`}
                color="info"
                variant="outlined"
              />
              <Chip
                label={currentTable.category.toUpperCase()}
                size="small"
                color="primary"
                variant="filled"
              />
              <Tooltip title="Satır sayılarını yeniden yükle">
                <IconButton size="small" onClick={fetchCounts} disabled={loadingCounts} data-testid="iconbutton-24262e">
                  {loadingCounts ? <CircularProgress size={16} /> : <RefreshIcon fontSize="small" />}
                </IconButton>
              </Tooltip>
            </Box>
          </Box>
        </CardContent>
      </Card>

      {/* Main Form (Left 7 cols) + Live Preview / History (Right 5 cols) */}
      <Box sx={{ display: 'grid', gridTemplateColumns: { xs: '1fr', lg: '7fr 5fr' }, gap: 3 }}>
        {/* Left Form Area */}
        <Paper sx={{ p: 3 }} variant="outlined">
          <Box sx={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', mb: 2 }}>
            <Box>
              <Typography variant="h6" sx={{ fontWeight: 600 }}>
                {currentTable.displayName}
              </Typography>
              <Typography variant="caption" color="text.secondary">
                {t('common:generator.requiredFieldsNotice')}
              </Typography>
            </Box>
            <Chip label={`${currentTable.fields.length} Alan`} size="small" variant="outlined" />
          </Box>

          <Divider sx={{ mb: 2.5 }} />

          <Box sx={{ display: 'grid', gridTemplateColumns: { xs: '1fr', sm: '1fr 1fr' }, gap: 2.5 }}>
            {currentTable.fields.map((field: FieldDefinition) => {
              const isLongText =
                field.name.toLowerCase().includes('description') ||
                field.name.toLowerCase().includes('notes') ||
                field.name.toLowerCase().includes('reason') ||
                field.name.toLowerCase().includes('payload') ||
                field.name.toLowerCase().includes('message')

              // 1. BOOLEAN FIELD
              if (field.type === 'boolean') {
                return (
                  <Box key={field.name} sx={{ gridColumn: { xs: '1', sm: 'span 1' } }}>
                    <Paper variant="outlined" sx={{ px: 2, py: 1, height: '100%', display: 'flex', alignItems: 'center' }}>
                      <FormControlLabel
                        control={
                          <Switch
                            checked={Boolean(formData[field.name])}
                            onChange={(e) => handleFieldChange(field.name, e.target.checked)}
                            data-testid={`switch-${field.name}`}
                          />
                        }
                        label={field.label}
                      />
                    </Paper>
                  </Box>
                )
              }

              // 2. ENUM FIELD (DROPDOWN WITH CHIPS)
              if (field.type === 'enum' && field.options) {
                return (
                  <Box key={field.name} sx={{ gridColumn: { xs: '1', sm: 'span 1' } }}>
                    <TextField
                      select
                      fullWidth
                      label={field.label}
                      value={formData[field.name] !== undefined ? formData[field.name] : ''}
                      onChange={(e) => handleFieldChange(field.name, e.target.value)}
                      helperText={field.helperText}
                      required={field.required}
                      data-testid={`select-${field.name}`}
                    >
                      {field.options.map((opt) => (
                        <MenuItem key={String(opt.value)} value={opt.value as string | number} data-testid="menuitem-2bd6e7">
                          <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                            <Chip label={opt.label} color={opt.color || 'default'} size="small" />
                            {opt.label !== String(opt.value) && (
                              <Typography variant="caption" color="text.secondary">
                                ({String(opt.value)})
                              </Typography>
                            )}
                          </Box>
                        </MenuItem>
                      ))}
                    </TextField>
                  </Box>
                )
              }

              // 3. FOREIGN KEY (FK) WITH CUSTOMER / USER SHORTCUTS
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
                        value={formData[field.name] || ''}
                        onChange={(e) => handleFieldChange(field.name, e.target.value)}
                        helperText={field.helperText || 'Mevcut müşterilerden seçim yapın veya kimlik girin'}
                        required={field.required}
                        data-testid={`fk-select-${field.name}`}
                      >
                        {customers.map((c) => (
                          <MenuItem key={c.id} value={c.id} data-testid="menuitem-609e36">
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
                        value={formData[field.name] || ''}
                        onChange={(e) => handleFieldChange(field.name, e.target.value)}
                        helperText={field.helperText || 'Sistem kullanıcılarından birini seçin'}
                        required={field.required}
                        data-testid={`fk-select-${field.name}`}
                      >
                        {users.map((u) => (
                          <MenuItem key={u.id} value={u.id} data-testid="menuitem-6adfdd">
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
                        value={formData[field.name] !== undefined ? formData[field.name] : ''}
                        onChange={(e) => handleFieldChange(field.name, e.target.value)}
                        helperText={field.helperText || 'İlgili kaydın UUID / GUID kimliği'}
                        required={field.required}
                        data-testid={`input-${field.name}`}
                      />
                    )}
                  </Box>
                )
              }

              // 4. NUMBER FIELD
              if (field.type === 'number') {
                return (
                  <Box key={field.name} sx={{ gridColumn: { xs: '1', sm: 'span 1' } }}>
                    <TextField
                      fullWidth
                      type="number"
                      label={field.label}
                      value={formData[field.name] !== undefined ? formData[field.name] : ''}
                      onChange={(e) =>
                        handleFieldChange(
                          field.name,
                          e.target.value === '' ? '' : Number(e.target.value)
                        )
                      }
                      helperText={field.helperText}
                      required={field.required}
                      data-testid={`input-${field.name}`}
                    />
                  </Box>
                )
              }

              // 5. DATE FIELD
              if (field.type === 'date') {
                return (
                  <Box key={field.name} sx={{ gridColumn: { xs: '1', sm: 'span 1' } }}>
                    <TextField
                      fullWidth
                      type="date"
                      label={field.label}
                      slotProps={{ inputLabel: { shrink: true } }}
                      value={formData[field.name] !== undefined ? formData[field.name] : ''}
                      onChange={(e) => handleFieldChange(field.name, e.target.value)}
                      helperText={field.helperText}
                      required={field.required}
                      data-testid={`input-${field.name}`}
                    />
                  </Box>
                )
              }

              // 6. DATETIME FIELD
              if (field.type === 'datetime') {
                return (
                  <Box key={field.name} sx={{ gridColumn: { xs: '1', sm: 'span 1' } }}>
                    <TextField
                      fullWidth
                      type="datetime-local"
                      label={field.label}
                      slotProps={{ inputLabel: { shrink: true } }}
                      value={formData[field.name] !== undefined ? formData[field.name] : ''}
                      onChange={(e) => handleFieldChange(field.name, e.target.value)}
                      helperText={field.helperText}
                      required={field.required}
                      data-testid={`input-${field.name}`}
                    />
                  </Box>
                )
              }

              // 7. DEFAULT TEXT FIELD
              return (
                <Box key={field.name} sx={{ gridColumn: { xs: '1', sm: isLongText ? 'span 2' : 'span 1' } }}>
                  <TextField
                    fullWidth
                    multiline={isLongText}
                    rows={isLongText ? 2 : 1}
                    label={field.label}
                    value={formData[field.name] !== undefined ? formData[field.name] : ''}
                    onChange={(e) => handleFieldChange(field.name, e.target.value)}
                    helperText={field.helperText}
                    required={field.required}
                    data-testid={`input-${field.name}`}
                  />
                </Box>
              )
            })}
          </Box>
        </Paper>

        {/* Right Info & Live Preview Area */}
        <Stack spacing={3}>
          {/* Live SQL / JSON Preview with Tabs */}
          <Card variant="outlined">
            <CardContent>
              <Box sx={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', mb: 1.5 }}>
                <Tabs
                  value={previewTab}
                  onChange={(_, val: number) => setPreviewTab(val)}
                  textColor="primary"
                  indicatorColor="primary"
                  sx={{ minHeight: 36 }}
                >
                  <Tab
                    icon={<CodeIcon fontSize="small" />}
                    iconPosition="start"
                    label="SQL INSERT"
                    sx={{ minHeight: 36, textTransform: 'none', py: 0.5 }}
                    data-testid="tab-sql-preview"
                  />
                  <Tab
                    icon={<DataObjectIcon fontSize="small" />}
                    iconPosition="start"
                    label="JSON Payload"
                    sx={{ minHeight: 36, textTransform: 'none', py: 0.5 }}
                    data-testid="tab-json-preview"
                  />
                </Tabs>

                <Tooltip title={copied ? t('common:generator.copySuccess') : 'Kopyala'}>
                  <IconButton size="small" onClick={handleCopy} data-testid="btn-copy-preview">
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
                  maxHeight: 280,
                  whiteSpace: 'pre-wrap',
                  wordBreak: 'break-all',
                }}
              >
                {previewTab === 0 ? generatedSql : jsonPreview}
              </Paper>
            </CardContent>
          </Card>

          {/* Database Info Notice */}
          <Card variant="outlined">
            <CardContent>
              <Box sx={{ display: 'flex', alignItems: 'center', gap: 1, mb: 1 }}>
                <StorageIcon color="primary" fontSize="small" />
                <Typography variant="subtitle2" sx={{ fontWeight: 600 }}>
                  {t('common:generator.databaseSource')}
                </Typography>
              </Box>
              <Typography variant="body2" color="text.secondary" sx={{ fontSize: '0.82rem' }}>
                {t('common:generator.directInsertNotice')}
              </Typography>
            </CardContent>
          </Card>

          {/* Generated History in this session */}
          <Card variant="outlined">
            <CardContent>
              <Typography variant="subtitle2" sx={{ fontWeight: 600, mb: 1.5 }}>
                {t('common:generator.recentRecords')} ({history.length})
              </Typography>
              {history.length === 0 ? (
                <Typography variant="caption" color="text.secondary">
                  {t('common:generator.noRecentRecords')}
                </Typography>
              ) : (
                <Stack spacing={1}>
                  {history.map((item, idx) => (
                    <Paper
                      key={idx}
                      variant="outlined"
                      sx={{ p: 1, display: 'flex', alignItems: 'center', justifyContent: 'space-between' }}
                    >
                      <Box sx={{ overflow: 'hidden', mr: 1 }}>
                        <Typography variant="caption" sx={{ fontWeight: 600, display: 'block', textTransform: 'uppercase' }} color="primary">
                          {item.table}
                        </Typography>
                        <Typography variant="body2" noWrap sx={{ fontSize: '0.82rem' }}>
                          {item.title}
                        </Typography>
                      </Box>
                      <Chip label={item.timestamp} size="small" variant="outlined" sx={{ fontSize: '0.7rem' }} />
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

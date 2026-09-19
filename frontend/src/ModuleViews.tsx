import { useEffect, useMemo, useState } from 'react'
import { Box, Typography, Button, TextField, InputAdornment, Paper, Table, TableBody, TableCell, TableContainer, TableHead, TableRow, Chip, IconButton, CircularProgress, Alert } from '@mui/material'
import SearchIcon from '@mui/icons-material/Search'
import FilterListIcon from '@mui/icons-material/FilterList'
import AddIcon from '@mui/icons-material/Add'
import VisibilityIcon from '@mui/icons-material/Visibility'
import { quotesApi } from './api'
import { CreateQuoteModal } from './CreateQuoteModal'
import { QuoteDetailDrawer } from './QuoteDetailDrawer'
import { useI18n } from './i18n'
import { formatMoney } from './common/format'
import type { Quote } from './api'

export function QuotesView() {
  const { translate: t, lang } = useI18n()
  const [query, setQuery] = useState('')
  const [items, setItems] = useState<Quote[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')
  const [showModal, setShowModal] = useState(false)
  const [reloadKey, setReloadKey] = useState(0)
  const [selectedQuote, setSelectedQuote] = useState<Quote | null>(null)

  useEffect(() => {
    let ignore = false
    quotesApi
      .list()
      .then((result) => {
        if (!ignore) {
          setItems(result)
          setError('')
          setLoading(false)
        }
      })
      .catch((reason: unknown) => {
        if (!ignore) {
          setError(reason instanceof Error ? reason.message : 'Error')
          setLoading(false)
        }
      })
    return () => {
      ignore = true
    }
  }, [reloadKey])

  const filtered = useMemo(
    () => items.filter((quote) => `${quote.number} ${quote.title}`.toLowerCase().includes(query.toLowerCase())),
    [items, query]
  )

  return (
    <Box>
      <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 3 }}>
        <Box>
          <Typography variant="overline" color="text.secondary">{t('common:views.quotesLabel')}</Typography>
          <Typography variant="h4">{t('common:views.quotesTitle')}</Typography>
        </Box>
        <Button variant="contained" startIcon={<AddIcon />} onClick={() => setShowModal(true)}>
          {t('common:modals.newQuote')}
        </Button>
      </Box>

      {loading && <Box sx={{ display: 'flex', justifyContent: 'center', p: 4 }}><CircularProgress /></Box>}
      {error && <Alert severity="error" sx={{ mb: 3 }}>{error}</Alert>}

      {!loading && !error && (
        <Paper sx={{ width: '100%', mb: 2 }}>
          <Box sx={{ p: 2, display: 'flex', gap: 2, alignItems: 'center' }}>
            <TextField
              size="small"
              placeholder="Search quotes..."
              value={query}
              onChange={(e) => setQuery(e.target.value)}
              slotProps={{ input: { startAdornment: <InputAdornment position="start"><SearchIcon /></InputAdornment> } }}
              sx={{ flexGrow: 1, maxWidth: 400 }}
            />
            <Button variant="outlined" startIcon={<FilterListIcon />}>{t('common:fields.filter')}</Button>
          </Box>
          <TableContainer>
            <Table>
              <TableHead>
                <TableRow>
                  <TableCell>{t('common:fields.number')}</TableCell>
                  <TableCell>{t('common:fields.title')}</TableCell>
                  <TableCell>{t('common:fields.customer')}</TableCell>
                  <TableCell>{t('common:fields.total')}</TableCell>
                  <TableCell>{t('common:fields.status')}</TableCell>
                  <TableCell align="right">{t('common:fields.actions')}</TableCell>
                </TableRow>
              </TableHead>
              <TableBody>
                {filtered.length === 0 ? (
                  <TableRow><TableCell colSpan={6} align="center">{t('common:common.noDataFound')}</TableCell></TableRow>
                ) : (
                  filtered.map((quote) => (
                    <TableRow key={quote.id} hover>
                      <TableCell><strong>{quote.number}</strong></TableCell>
                      <TableCell>{quote.title}</TableCell>
                      <TableCell>{quote.customerId}</TableCell>
                      <TableCell>{formatMoney(quote.total, lang)}</TableCell>
                      <TableCell>
                        <Chip label={quote.state} size="small" color={quote.state === 'Sent' ? 'primary' : quote.state === 'Accepted' ? 'success' : 'default'} />
                      </TableCell>
                      <TableCell align="right">
                        <IconButton size="small" onClick={() => setSelectedQuote(quote)} title={t('common:views.viewDetails')}>
                          <VisibilityIcon />
                        </IconButton>
                      </TableCell>
                    </TableRow>
                  ))
                )}
              </TableBody>
            </Table>
          </TableContainer>
        </Paper>
      )}

      {showModal && (
        <CreateQuoteModal
          onClose={() => setShowModal(false)}
          onSuccess={() => {
            setShowModal(false)
            setReloadKey((k) => k + 1)
          }}
        />
      )}
      
      {selectedQuote && (
        <QuoteDetailDrawer quote={selectedQuote} onClose={() => setSelectedQuote(null)} />
      )}
    </Box>
  )
}

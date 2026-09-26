import { useState, useEffect } from 'react'
import {
  Box, Typography, Button, TextField, Select, MenuItem, InputAdornment,
  List, ListItemButton, Chip, Divider, useMediaQuery, useTheme,
  IconButton, CircularProgress, Stack
} from '@mui/material'
import SearchIcon from '@mui/icons-material/Search'
import AddIcon from '@mui/icons-material/Add'
import CloseIcon from '@mui/icons-material/Close'
import FilterListIcon from '@mui/icons-material/FilterList'
import AutoFixHighIcon from '@mui/icons-material/AutoFixHigh'
import { useI18n } from '../../i18n'
import { servicesApi, type ServiceStatus, type ServiceSummaryDto } from '../../api/services'
import { type ApiPage } from '../../api/_base'
import { ServiceDetailPanel } from './ServiceDetailPanel'
import { CreateServicePanel } from './CreateServicePanel'
import { formatMoney } from '../../common/format'

export function ServicesView() {
  const { translate: t } = useI18n()
  const theme = useTheme()
  const isMobile = useMediaQuery(theme.breakpoints.down('md'))
  
  const [selectedId, setSelectedId] = useState<string | null>(null)
  const [isCreating, setIsCreating] = useState(false)
  
  const [search, setSearch] = useState('')
  const [statusFilter, setStatusFilter] = useState<string>('')
  
  const [data, setData] = useState<ApiPage<ServiceSummaryDto> | null>(null)
  const [loading, setLoading] = useState(false)

  const reload = () => {
    setLoading(true)
    servicesApi.list({ search, status: statusFilter || undefined, limit: 50 })
      .then(setData)
      .finally(() => setLoading(false))
  }

  // eslint-disable-next-line react-hooks/exhaustive-deps
  useEffect(() => { reload() }, [search, statusFilter])

  const fmtDate = (d: string) => new Date(d).toLocaleDateString()

  const showRightPanel = selectedId !== null || isCreating

  const handleSelect = (id: string) => {
    setSelectedId(id)
    setIsCreating(false)
  }

  const handleCreateNew = () => {
    setSelectedId(null)
    setIsCreating(true)
  }

  const handleCloseRightPanel = () => {
    setSelectedId(null)
    setIsCreating(false)
  }

  const getStatusColor = (status: ServiceStatus): 'default' | 'primary' | 'secondary' | 'error' | 'info' | 'success' | 'warning' => {
    switch (status) {
      case 'Draft': return 'default'
      case 'Quoted': return 'info'
      case 'Accepted': return 'primary'
      case 'InProgress': return 'warning'
      case 'Completed': return 'success'
      case 'Invoiced': return 'success'
      case 'Cancelled': return 'error'
      case 'Rejected': return 'error'
      case 'OnHold': return 'secondary'
      default: return 'default'
    }
  }

  return (
    <Box sx={{ display: 'flex', height: 'calc(100vh - 64px)', overflow: 'hidden', bgcolor: 'background.default' }}>
      {/* MIDDLE PANEL (Pool) */}
      <Box sx={{ 
        width: isMobile ? '100%' : '50%',
        display: (isMobile && showRightPanel) ? 'none' : 'flex',
        flexDirection: 'column',
        borderRight: 1, 
        borderColor: 'divider',
        height: '100%',
        bgcolor: 'background.paper'
      }}>
        <Box sx={{ p: 3, borderBottom: 1, borderColor: 'divider' }}>
          <Stack direction="row" sx={{ justifyContent: 'space-between', alignItems: 'center', mb: 3 }}>
            <Typography variant="h5" sx={{ fontWeight: 'bold' }}>
              {t('services:title')}
            </Typography>
            <Button 
              variant="contained" 
              startIcon={<AddIcon />} 
              onClick={handleCreateNew}
              disableElevation
              sx={{ borderRadius: 2 }}
            >
              {t('services:createNew')}
            </Button>
          </Stack>
          
          <Stack direction="row" spacing={2}>
            <TextField 
              fullWidth
              size="small"
              placeholder={t('services:search')}
              value={search}
              onChange={(e) => setSearch(e.target.value)}
              slotProps={{
                input: {
                  startAdornment: <InputAdornment position="start"><SearchIcon /></InputAdornment>
                } as any
              }}
            />
            <Select
              size="small"
              displayEmpty
              value={statusFilter}
              onChange={(e) => setStatusFilter(e.target.value)}
              startAdornment={<InputAdornment position="start"><FilterListIcon fontSize="small" /></InputAdornment>}
              sx={{ minWidth: 160 }}
            >
              <MenuItem value="">{t('common:all' as any) || 'All'}</MenuItem>
              <MenuItem value="Draft">{t('services:status.Draft')}</MenuItem>
              <MenuItem value="Quoted">{t('services:status.Quoted')}</MenuItem>
              <MenuItem value="Accepted">{t('services:status.Accepted')}</MenuItem>
              <MenuItem value="InProgress">{t('services:status.InProgress')}</MenuItem>
              <MenuItem value="Completed">{t('services:status.Completed')}</MenuItem>
              <MenuItem value="Invoiced">{t('services:status.Invoiced')}</MenuItem>
              <MenuItem value="Cancelled">{t('services:status.Cancelled')}</MenuItem>
            </Select>
          </Stack>
        </Box>

        <Box sx={{ flex: 1, overflowY: 'auto' }}>
          {loading && <Box sx={{ p: 3, textAlign: 'center' }}><CircularProgress size={24} /></Box>}
          {!loading && data?.items.length === 0 && (
            <Box sx={{ p: 4, textAlign: 'center', color: 'text.secondary' }}>
              <AutoFixHighIcon sx={{ fontSize: 48, mb: 2, opacity: 0.5 }} />
              <Typography>{t('common:noResults' as any) || 'No results'}</Typography>
            </Box>
          )}
          <List disablePadding>
            {data?.items.map(s => (
              <Box key={s.id}>
                <ListItemButton 
                  selected={selectedId === s.id}
                  onClick={() => handleSelect(s.id)}
                  sx={{ p: 3, '&.Mui-selected': { bgcolor: 'action.selected' } }}
                >
                  <Box sx={{ width: '100%' }}>
                    <Stack direction="row" sx={{ justifyContent: 'space-between', mb: 1 }}>
                      <Typography variant="subtitle1" noWrap sx={{ maxWidth: '70%', fontWeight: 'bold' }}>
                        {s.title || s.number}
                      </Typography>
                      <Typography variant="body2" color="text.secondary">
                        {fmtDate(s.createdAt)}
                      </Typography>
                    </Stack>
                    <Stack direction="row" sx={{ justifyContent: 'space-between', alignItems: 'center' }}>
                      <Typography variant="body2" color="text.secondary" noWrap sx={{ maxWidth: '60%' }}>
                        #{s.number} • {s.customerId.substring(0, 8)}...
                      </Typography>
                      <Typography variant="subtitle2" sx={{ fontWeight: 'bold' }}>
                        {formatMoney(s.currentTotal, 'tr')}
                      </Typography>
                    </Stack>
                    <Stack direction="row" spacing={1} sx={{ mt: 1.5 }}>
                      <Chip 
                        label={t(`services:status.${s.status}` as any) || s.status} 
                        size="small" 
                        color={getStatusColor(s.status)}
                        sx={{ fontWeight: 'bold', borderRadius: 1 }}
                      />
                      {s.subStatus && s.subStatus !== 'None' && (
                        <Chip 
                          label={t(`services:subStatus.${s.subStatus}` as any) || s.subStatus} 
                          size="small" 
                          variant="outlined"
                          sx={{ borderRadius: 1 }}
                        />
                      )}
                    </Stack>
                  </Box>
                </ListItemButton>
                <Divider />
              </Box>
            ))}
          </List>
        </Box>
      </Box>

      {/* RIGHT PANEL (Detail/Create) */}
      <Box sx={{ 
        width: isMobile ? '100%' : '50%',
        display: (isMobile && !showRightPanel) ? 'none' : 'flex',
        flexDirection: 'column',
        height: '100%',
        bgcolor: 'background.default',
        position: 'relative'
      }}>
        {!showRightPanel && (
          <Box sx={{ display: 'flex', alignItems: 'center', justifyContent: 'center', height: '100%', p: 4, textAlign: 'center', color: 'text.secondary' }}>
            <Box>
              <AutoFixHighIcon sx={{ fontSize: 64, opacity: 0.2, mb: 2 }} />
              <Typography variant="h6">{t('services:detail.selectToView')}</Typography>
            </Box>
          </Box>
        )}

        {showRightPanel && (
          <Box sx={{ display: 'flex', flexDirection: 'column', height: '100%' }}>
            {isMobile && (
              <Box sx={{ p: 2, borderBottom: 1, borderColor: 'divider', display: 'flex', alignItems: 'center', bgcolor: 'background.paper' }}>
                <IconButton onClick={handleCloseRightPanel} edge="start">
                  <CloseIcon />
                </IconButton>
                <Typography sx={{ ml: 1, fontWeight: 'bold' }}>{t('common:back', 'Back')}</Typography>
              </Box>
            )}

            <Box sx={{ flex: 1, overflowY: 'auto', p: isMobile ? 2 : 4 }}>
              {isCreating ? (
                <CreateServicePanel onCreated={(id) => { reload(); handleSelect(id); }} onCancel={handleCloseRightPanel} />
              ) : selectedId ? (
                <ServiceDetailPanel serviceId={selectedId} onUpdated={reload} />
              ) : null}
            </Box>
          </Box>
        )}
      </Box>
    </Box>
  )
}

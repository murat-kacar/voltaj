import { useEffect, useState } from 'react'
import {
  Alert, Box, Button, Chip, Dialog, DialogActions, DialogContent, DialogTitle,
  Divider, Drawer, IconButton, Paper, TextField, Typography,
} from '@mui/material'
import AddIcon from '@mui/icons-material/Add'
import CloseIcon from '@mui/icons-material/Close'
import { DataGrid, type GridColDef } from '@mui/x-data-grid'
import { projectsApi, billingApi, type ProjectDto, type BillingEntryDto, type Customer } from '../api'
import { CustomerPicker } from '../customers/CustomerPicker'
import { useI18n } from '../i18n'

export function ProjectsView() {
  const { translate: t } = useI18n()
  const [projects, setProjects] = useState<ProjectDto[]>([])
  const [loading, setLoading] = useState(true)
  const [listError, setListError] = useState('')
  const [reload, setReload] = useState(0)

  const [showCreate, setShowCreate] = useState(false)
  const [createCustomer, setCreateCustomer] = useState<Customer | null>(null)
  const [createName, setCreateName] = useState('')
  const [createBudget, setCreateBudget] = useState('')
  const [creating, setCreating] = useState(false)
  const [createError, setCreateError] = useState('')

  const [selected, setSelected] = useState<ProjectDto | null>(null)
  const [billing, setBilling] = useState<BillingEntryDto[]>([])
  const [billingLoading, setBillingLoading] = useState(false)

  const [phaseTitle, setPhaseTitle] = useState('')
  const [phasePlanned, setPhasePlanned] = useState('')
  const [addingPhase, setAddingPhase] = useState(false)
  const [phaseError, setPhaseError] = useState('')

  const [billingAmount, setBillingAmount] = useState('')
  const [addingBilling, setAddingBilling] = useState(false)
  const [billingError, setBillingError] = useState('')

  useEffect(() => {
    let ignore = false
    projectsApi.list()
      .then(page => { if (!ignore) { setProjects(page.items); setLoading(false) } })
      .catch(() => { if (!ignore) { setListError(t('projects:errors.loadFailed')); setLoading(false) } })
    return () => { ignore = true }
  }, [reload, t])

  useEffect(() => {
    if (!selected) return
    let ignore = false
    billingApi.list(selected.id)
      .then(page => { if (!ignore) { setBilling(page.items); setBillingLoading(false) } })
      .catch(() => { if (!ignore) setBillingLoading(false) })
    return () => { ignore = true }
  }, [selected])

  const handleCreate = async () => {
    if (!createCustomer) { setCreateError(t('projects:validation.customerRequired')); return }
    if (!createName.trim()) { setCreateError(t('projects:validation.nameRequired')); return }
    const budget = parseFloat(createBudget)
    if (isNaN(budget) || budget <= 0) { setCreateError(t('projects:validation.budgetRequired')); return }
    setCreating(true); setCreateError('')
    try {
      await projectsApi.create({ customerId: createCustomer.id, name: createName.trim(), budget })
      setShowCreate(false); setCreateCustomer(null); setCreateName(''); setCreateBudget('')
      setLoading(true); setListError('')
      setReload(r => r + 1)
    } catch (err) {
      setCreateError(err instanceof Error ? err.message : t('projects:errors.createFailed'))
    } finally {
      setCreating(false)
    }
  }

  const handleAddPhase = async () => {
    if (!selected) return
    if (!phaseTitle.trim()) { setPhaseError(t('projects:validation.phaseRequired')); return }
    const planned = parseFloat(phasePlanned)
    if (isNaN(planned) || planned < 0) { setPhaseError(t('projects:validation.phaseRequired')); return }
    setAddingPhase(true); setPhaseError('')
    try {
      const updated = await projectsApi.addPhase(selected.id, { title: phaseTitle.trim(), plannedAmount: planned })
      setSelected(updated)
      setProjects(ps => ps.map(p => p.id === updated.id ? updated : p))
      setPhaseTitle(''); setPhasePlanned('')
    } catch (err) {
      setPhaseError(err instanceof Error ? err.message : t('projects:errors.phaseFailed'))
    } finally {
      setAddingPhase(false)
    }
  }

  const handleAddBilling = async () => {
    if (!selected) return
    const amount = parseFloat(billingAmount)
    if (isNaN(amount) || amount <= 0) { setBillingError(t('projects:validation.billingRequired')); return }
    setAddingBilling(true); setBillingError('')
    try {
      const entry = await billingApi.create(selected.id, { customerId: selected.customerId, amount })
      setBilling(b => [entry, ...b])
      setBillingAmount('')
    } catch (err) {
      setBillingError(err instanceof Error ? err.message : t('projects:errors.billingFailed'))
    } finally {
      setAddingBilling(false)
    }
  }

  const columns: GridColDef[] = [
    { field: 'number', headerName: t('projects:table.number'), width: 160 },
    { field: 'name', headerName: t('projects:table.name'), flex: 1 },
    { field: 'budget', headerName: t('projects:table.budget'), width: 130, type: 'number' },
    {
      field: 'phases', headerName: t('projects:table.phases'), width: 90,
      valueGetter: (_val, row) => (row as ProjectDto).phases.length,
      renderCell: (params) => <Chip label={params.value} size="small" />,
    },
  ]

  const phaseColumns: GridColDef[] = [
    { field: 'title', headerName: 'Title', flex: 1 },
    { field: 'plannedAmount', headerName: 'Planned', width: 130, type: 'number' },
  ]

  const billingColumns: GridColDef[] = [
    { field: 'amount', headerName: 'Amount', width: 130, type: 'number' },
  ]

  return (
    <Box>
      <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 3 }}>
        <Box>
          <Typography variant="overline" color="text.secondary">{t('projects:eyebrow')}</Typography>
          <Typography variant="h4">{t('projects:title')}</Typography>
          <Typography variant="body2" color="text.secondary">{t('projects:subtitle')}</Typography>
        </Box>
        <Button variant="contained" startIcon={<AddIcon />} onClick={() => setShowCreate(true)} data-testid="button-295475">
          {t('projects:newProject')}
        </Button>
      </Box>

      {listError && <Alert severity="error" sx={{ mb: 2 }}>{listError}</Alert>}

      <Paper sx={{ width: '100%', height: 480 }}>
        <DataGrid
          rows={projects}
          columns={columns}
          loading={loading}
          initialState={{ pagination: { paginationModel: { pageSize: 10 } } }}
          pageSizeOptions={[10, 25, 50]}
          onRowClick={({ row }) => { setBillingLoading(true); setSelected(row as ProjectDto) }}
          sx={{ cursor: 'pointer' }}
        />
      </Paper>

      {/* Create project dialog */}
      <Dialog open={showCreate} onClose={() => setShowCreate(false)} maxWidth="sm" fullWidth data-testid="dialog-cfbbfb">
        <DialogTitle>
          <Typography variant="h6">{t('projects:form.title')}</Typography>
          <Typography variant="body2" color="text.secondary">{t('projects:form.subtitle')}</Typography>
        </DialogTitle>
        <DialogContent sx={{ display: 'flex', flexDirection: 'column', gap: 2, pt: '16px !important' }}>
          {createError && <Alert severity="error">{createError}</Alert>}
          <CustomerPicker value={createCustomer} onChange={setCreateCustomer} label={t('projects:form.customer')} />
          <TextField
            label={t('projects:form.name')}
            placeholder={t('projects:form.namePlaceholder')}
            value={createName}
            onChange={e => setCreateName(e.target.value)}
            fullWidth size="small"
           data-testid="textfield-ab0238" />
          <TextField
            label={t('projects:form.budget')}
            type="number"
            value={createBudget}
            onChange={e => setCreateBudget(e.target.value)}
            fullWidth size="small"
           data-testid="textfield-1498f2" />
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setShowCreate(false)} data-testid="button-4a9998">{t('projects:form.cancel')}</Button>
          <Button variant="contained" onClick={handleCreate} disabled={creating} data-testid="button-0827fa">
            {creating ? t('projects:form.creating') : t('projects:form.submit')}
          </Button>
        </DialogActions>
      </Dialog>

      {/* Detail drawer */}
      <Drawer anchor="right" open={!!selected} onClose={() => setSelected(null)}
        slotProps={{ paper: { sx: { width: { xs: '100%', sm: 480 }, p: 3 } } }}>
        {selected && (
          <Box>
            <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start', mb: 2 }}>
              <Box>
                <Typography variant="overline" color="text.secondary">{selected.number}</Typography>
                <Typography variant="h6">{selected.name}</Typography>
                <Typography variant="body2" color="text.secondary">
                  {t('projects:drawer.budget')}: {selected.budget.toLocaleString()}
                </Typography>
              </Box>
              <IconButton onClick={() => setSelected(null)} data-testid="iconbutton-76a9cc"><CloseIcon /></IconButton>
            </Box>
            <Divider sx={{ mb: 2 }} />
            <Box component="section" sx={{ mb: 3 }}>
              <Typography variant="h6" component="h3" gutterBottom>{t('projects:drawer.phases')}</Typography>
              <Paper sx={{ height: 200, mb: 2 }}>
                <DataGrid
                  rows={selected.phases}
                  columns={phaseColumns}
                  hideFooter
                  density="compact"
                  disableRowSelectionOnClick
                  localeText={{ noRowsLabel: t('projects:drawer.noPhases') }}
                />
              </Paper>
              <Typography variant="subtitle2" sx={{ mb: 1 }}>{t('projects:drawer.addPhase')}</Typography>
              {phaseError && <Alert severity="error" sx={{ mb: 1 }}>{phaseError}</Alert>}
              <Box sx={{ display: 'flex', flexDirection: 'column', gap: 1 }}>
                <TextField
                  label={t('projects:drawer.phaseTitle')}
                  placeholder={t('projects:drawer.phaseTitlePlaceholder')}
                  value={phaseTitle}
                  onChange={e => setPhaseTitle(e.target.value)}
                  size="small" fullWidth
                 data-testid="textfield-e25236" />
                <TextField
                  label={t('projects:drawer.plannedAmount')}
                  type="number"
                  value={phasePlanned}
                  onChange={e => setPhasePlanned(e.target.value)}
                  size="small" fullWidth
                 data-testid="textfield-4c57e5" />
                <Button variant="outlined" onClick={handleAddPhase} disabled={addingPhase} data-testid="button-af5468">
                  {t('projects:drawer.savePhase')}
                </Button>
              </Box>
            </Box>

            <Divider sx={{ mb: 3 }} />

            <Box component="section">
              <Typography variant="h6" component="h3" gutterBottom>{t('projects:drawer.billing')}</Typography>
              <Paper sx={{ height: 200, mb: 2 }}>
                <DataGrid
                  rows={billing}
                  columns={billingColumns}
                  loading={billingLoading}
                  hideFooter
                  density="compact"
                  disableRowSelectionOnClick
                  localeText={{ noRowsLabel: t('projects:drawer.noBilling') }}
                />
              </Paper>
              <Typography variant="subtitle2" sx={{ mb: 1 }}>{t('projects:drawer.recordBilling')}</Typography>
              {billingError && <Alert severity="error" sx={{ mb: 1 }}>{billingError}</Alert>}
              <Box sx={{ display: 'flex', gap: 1, alignItems: 'flex-start' }}>
                <TextField
                  label={t('projects:drawer.billingAmount')}
                  type="number"
                  value={billingAmount}
                  onChange={e => setBillingAmount(e.target.value)}
                  size="small"
                  sx={{ flex: 1 }}
                 data-testid="textfield-93f6a5" />
                <Button variant="outlined" onClick={handleAddBilling} disabled={addingBilling} sx={{ mt: '2px' }} data-testid="button-9d2f2f">
                  {t('projects:drawer.saveBilling')}
                </Button>
              </Box>
            </Box>
          </Box>
        )}
      </Drawer>
    </Box>
  )
}

import { useEffect, useState } from 'react'
import { Alert, Box, Button, Chip, CircularProgress, FormControlLabel, IconButton, Paper, Stack, Switch, TextField, Typography } from '@mui/material'
import AddIcon from '@mui/icons-material/Add'
import EditIcon from '@mui/icons-material/Edit'
import { customersApi, type Customer, type CustomerAsset, type CustomerSite } from '../api'
import { errorText } from '../common/errors'
import { FormDialog } from '../common/FormDialog'
import { formatDate } from '../i18n/formatters'
import { useI18n } from '../i18n'

/** The addresses of a customer and the devices installed at each of them. */
export function SitesPanel({ customer, canEdit }: { customer: Customer; canEdit: boolean }) {
  const { translate: t, lang } = useI18n()
  const [sites, setSites] = useState<CustomerSite[] | null>(null)
  const [error, setError] = useState('')
  const [reloadKey, setReloadKey] = useState(0)
  const [siteDialog, setSiteDialog] = useState<CustomerSite | 'new' | null>(null)
  const [assetDialog, setAssetDialog] = useState<{ site: CustomerSite; asset: CustomerAsset | null } | null>(null)

  useEffect(() => {
    let ignore = false
    customersApi
      .sites(customer.id)
      .then((loaded) => {
        if (ignore) return
        setSites(loaded)
        setError('')
      })
      .catch((reason: unknown) => {
        if (!ignore) setError(errorText(reason))
      })
    return () => {
      ignore = true
    }
  }, [customer.id, reloadKey])

  const reload = () => setReloadKey((key) => key + 1)

  return (
    <Stack spacing={2}>
      {canEdit && (
        <Box>
          <Button startIcon={<AddIcon />} onClick={() => setSiteDialog('new')} data-testid="button-58aade">{t('customers:sites.add')}</Button>
        </Box>
      )}
      {error && <Alert severity="error">{error}</Alert>}
      {!sites && !error && <Box sx={{ display: 'flex', justifyContent: 'center', p: 2 }}><CircularProgress /></Box>}
      {sites?.length === 0 && <Typography color="text.secondary">{t('customers:sites.empty')}</Typography>}

      {sites?.map((site) => (
        <Paper key={site.id} variant="outlined" sx={{ p: 2 }}>
          <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start', gap: 1 }}>
            <Box sx={{ minWidth: 0 }}>
              <Typography variant="subtitle1" sx={{ fontWeight: 600 }}>
                {site.name}{' '}
                {!site.isActive && <Chip size="small" label={t('customers:sites.inactive')} />}
              </Typography>
              <Typography variant="body2" color="text.secondary" sx={{ whiteSpace: 'pre-line' }}>{site.address}</Typography>
            </Box>
            {canEdit && (
              <IconButton size="small" title={t('customers:sites.edit')} onClick={() => setSiteDialog(site)} data-testid="iconbutton-5d02d9"><EditIcon fontSize="small" /></IconButton>
            )}
          </Box>

          <Stack spacing={0.5} sx={{ mt: 1.5 }}>
            {site.assets.length === 0 && <Typography variant="caption" color="text.secondary">{t('customers:sites.noAssets')}</Typography>}
            {site.assets.map((asset) => (
              <Box key={asset.id} sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                <Box sx={{ flex: 1, minWidth: 0 }}>
                  <Typography variant="body2">
                    {asset.name}{' '}
                    {!asset.isActive && <Chip size="small" label={t('customers:sites.inactive')} />}
                  </Typography>
                  <Typography variant="caption" color="text.secondary">
                    {[
                      asset.serialNumber,
                      asset.installationDate ? `${t('customers:sites.installed')}: ${formatDate(asset.installationDate, lang, { year: 'numeric', month: 'short', day: 'numeric' })}` : '',
                    ].filter(Boolean).join(' · ')}
                  </Typography>
                </Box>
                {canEdit && (
                  <IconButton size="small" title={t('customers:sites.edit')} onClick={() => setAssetDialog({ site, asset })} data-testid="iconbutton-7424b7"><EditIcon fontSize="small" /></IconButton>
                )}
              </Box>
            ))}
          </Stack>
          {canEdit && (
            <Button size="small" startIcon={<AddIcon />} sx={{ mt: 1 }} onClick={() => setAssetDialog({ site, asset: null })} data-testid="button-808dcc">{t('customers:sites.addAsset')}</Button>
          )}
        </Paper>
      ))}

      {siteDialog && (
        <SiteDialog
          customerId={customer.id}
          site={siteDialog === 'new' ? null : siteDialog}
          onClose={() => setSiteDialog(null)}
          onSaved={() => {
            setSiteDialog(null)
            reload()
          }}
        />
      )}
      {assetDialog && (
        <AssetDialog
          customerId={customer.id}
          site={assetDialog.site}
          asset={assetDialog.asset}
          onClose={() => setAssetDialog(null)}
          onSaved={() => {
            setAssetDialog(null)
            reload()
          }}
        />
      )}
    </Stack>
  )
}

function SiteDialog({ customerId, site, onClose, onSaved }: { customerId: string; site: CustomerSite | null; onClose: () => void; onSaved: () => void }) {
  const { translate: t } = useI18n()
  const [name, setName] = useState(site?.name ?? '')
  const [address, setAddress] = useState(site?.address ?? '')
  const [isActive, setIsActive] = useState(site?.isActive ?? true)

  return (
    <FormDialog
      title={site ? t('customers:sites.editTitle') : t('customers:sites.addTitle')}
      submitLabel={t('common:actions.save')}
      canSubmit={name.trim() !== '' && address.trim() !== ''}
      onClose={onClose}
      onSubmit={async () => {
        const payload = { name: name.trim(), address: address.trim(), isActive }
        if (site) await customersApi.updateSite(customerId, site.id, payload)
        else await customersApi.createSite(customerId, payload)
        onSaved()
      }}
    >
      <TextField required autoFocus label={t('customers:sites.name')} value={name} onChange={(event) => setName(event.target.value)}  data-testid="textfield-2e4c28" />
      <TextField required multiline minRows={2} label={t('customers:sites.address')} value={address} onChange={(event) => setAddress(event.target.value)}  data-testid="textfield-48f71a" />
      {site && <FormControlLabel control={<Switch checked={isActive} onChange={(event) => setIsActive(event.target.checked)} />} label={t('customers:sites.active')} />}
    </FormDialog>
  )
}

function AssetDialog({ customerId, site, asset, onClose, onSaved }: { customerId: string; site: CustomerSite; asset: CustomerAsset | null; onClose: () => void; onSaved: () => void }) {
  const { translate: t } = useI18n()
  const [name, setName] = useState(asset?.name ?? '')
  const [serialNumber, setSerialNumber] = useState(asset?.serialNumber ?? '')
  const [installationDate, setInstallationDate] = useState(asset?.installationDate ?? '')
  const [isActive, setIsActive] = useState(asset?.isActive ?? true)

  return (
    <FormDialog
      title={`${asset ? t('customers:sites.editAssetTitle') : t('customers:sites.addAssetTitle')} · ${site.name}`}
      submitLabel={t('common:actions.save')}
      canSubmit={name.trim() !== ''}
      onClose={onClose}
      onSubmit={async () => {
        const payload = { name: name.trim(), serialNumber: serialNumber.trim() || undefined, installationDate: installationDate || null, isActive }
        if (asset) await customersApi.updateAsset(customerId, site.id, asset.id, payload)
        else await customersApi.createAsset(customerId, site.id, payload)
        onSaved()
      }}
    >
      <TextField required autoFocus label={t('customers:sites.assetName')} value={name} onChange={(event) => setName(event.target.value)}  data-testid="textfield-c6353a" />
      <TextField label={t('customers:sites.serial')} value={serialNumber} onChange={(event) => setSerialNumber(event.target.value)}  data-testid="textfield-e38e9e" />
      <TextField
        type="date"
        label={t('customers:sites.installed')}
        value={installationDate}
        onChange={(event) => setInstallationDate(event.target.value)}
        slotProps={{ inputLabel: { shrink: true } }}
       data-testid="textfield-3a6475" />
      {asset && <FormControlLabel control={<Switch checked={isActive} onChange={(event) => setIsActive(event.target.checked)} />} label={t('customers:sites.active')} />}
    </FormDialog>
  )
}

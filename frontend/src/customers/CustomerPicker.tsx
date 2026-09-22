import { useEffect, useState } from 'react'
import { Autocomplete, Box, TextField, Typography } from '@mui/material'
import { customersApi, type Customer } from '../api'
import { useI18n } from '../i18n'

type Props = {
  value: Customer | null
  onChange: (customer: Customer | null) => void
  label: string
  size?: 'small' | 'medium'
}

/**
 * Picks an active customer by typing part of the name, phone, email or tax number. The server does the searching, so the
 * list stays short and any number of customers work; the ones that are set inactive are left out.
 */
export function CustomerPicker({ value, onChange, label, size = 'small' }: Props) {
  const { translate: t } = useI18n()
  const [input, setInput] = useState('')
  const [options, setOptions] = useState<Customer[]>([])
  const [loading, setLoading] = useState(true)

  useEffect(() => {
    let ignore = false
    const handle = window.setTimeout(() => {
      setLoading(true)
      customersApi
        .page({ search: input.trim() || undefined, active: true, limit: 20 })
        .then((page) => {
          if (!ignore) setOptions(page.items)
        })
        .catch(() => {
          if (!ignore) setOptions([])
        })
        .finally(() => {
          if (!ignore) setLoading(false)
        })
    }, input === '' ? 0 : 250)
    return () => {
      ignore = true
      window.clearTimeout(handle)
    }
  }, [input])

  return (
    <Autocomplete
      size={size}
      options={options}
      value={value}
      loading={loading}
      loadingText={t('common:actions.loading')}
      noOptionsText={t('customers:table.empty')}
      onChange={(_, next) => onChange(next)}
      // only what is typed narrows the list; choosing an option must not shrink it to that option
      onInputChange={(_, text, reason) => {
        if (reason === 'input') setInput(text)
      }}
      filterOptions={(all) => all}
      getOptionLabel={(option) => option.fullName}
      isOptionEqualToValue={(option, selected) => option.id === selected.id}
      renderOption={(props, option) => (
        <Box component="li" {...props} key={option.id} sx={{ display: 'flex', flexDirection: 'column', alignItems: 'flex-start !important' }}>
          <Typography variant="body2">{option.fullName}</Typography>
          <Typography variant="caption" color="text.secondary">{[option.phone, option.email].filter(Boolean).join(' · ')}</Typography>
        </Box>
      )}
      renderInput={(params) => <TextField {...params} label={label}  data-testid="textfield-97362e" />}
    />
  )
}

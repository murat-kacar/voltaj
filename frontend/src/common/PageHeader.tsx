import type { ReactNode } from 'react'
import { Box, Typography } from '@mui/material'

/** The top of a screen: a small line above, the title, and what can be done on it (buttons, a status) at the right. */
export function PageHeader({ overline, title, actions }: { overline?: string; title: string; actions?: ReactNode }) {
  return (
    <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start', mb: 2, flexWrap: 'wrap', gap: 1 }}>
      <Box>
        {overline && <Typography variant="overline" color="text.secondary">{overline}</Typography>}
        <Typography variant="h4">{title}</Typography>
      </Box>
      {actions}
    </Box>
  )
}

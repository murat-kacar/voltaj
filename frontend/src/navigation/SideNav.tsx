import { useState } from 'react'
import { Accordion, AccordionDetails, AccordionSummary, Badge, Box, List, ListItemButton, ListItemIcon, ListItemText } from '@mui/material'
import ExpandMoreIcon from '@mui/icons-material/ExpandMore'
import { findGroupId, type NavGroup } from './navModel'

type Props = {
  groups: NavGroup[]
  activeView: string
  onNavigate: (viewId: string) => void
  ariaLabel: string
}

/** The left menu: one accordion group per module, its screens listed inside. */
export function SideNav({ groups, activeView, onNavigate, ariaLabel }: Props) {
  const activeGroupId = findGroupId(groups, activeView)
  const [expanded, setExpanded] = useState<string | false>(activeGroupId ?? false)

  // a view reached from somewhere else than the menu opens its own group
  const [seenGroupId, setSeenGroupId] = useState(activeGroupId)
  if (seenGroupId !== activeGroupId) {
    setSeenGroupId(activeGroupId)
    if (activeGroupId) setExpanded(activeGroupId)
  }

  return (
    <Box component="nav" aria-label={ariaLabel}>
      {groups.map((group) => {
        const active = group.id === activeGroupId
        const icon = group.badge ? <Badge badgeContent={group.badge} color="primary">{group.icon}</Badge> : group.icon

        if (!group.items) {
          return (
            <ListItemButton key={group.id} selected={active} onClick={() => onNavigate(group.id)} data-testid={`nav-item-${group.id}`}>
              <ListItemIcon>{icon}</ListItemIcon>
              <ListItemText primary={group.label} />
            </ListItemButton>
          )
        }

        return (
          <Accordion
            key={group.id}
            expanded={expanded === group.id}
            onChange={(_, isOpen) => setExpanded(isOpen ? group.id : false)}
            disableGutters
            square
            elevation={0}
            data-testid={`nav-group-${group.id}`}
            sx={{ bgcolor: 'transparent', '&::before': { display: 'none' } }}
          >
            <AccordionSummary
              expandIcon={<ExpandMoreIcon />}
              id={`nav-${group.id}-header`}
              aria-controls={`nav-${group.id}-items`}
              sx={{ color: active ? 'primary.main' : 'text.primary' }}
            >
              <ListItemIcon sx={{ color: 'inherit' }}>{icon}</ListItemIcon>
              <ListItemText primary={group.label} />
            </AccordionSummary>
            <AccordionDetails sx={{ p: 0 }}>
              <List disablePadding>
                {group.items.map((item) => (
                  <ListItemButton
                    key={item.id}
                    selected={item.id === activeView}
                    onClick={() => onNavigate(item.id)}
                    data-testid={`nav-item-${item.id}`}
                    sx={{ pl: 9 }}
                  >
                    <ListItemText primary={item.label} />
                  </ListItemButton>
                ))}
              </List>
            </AccordionDetails>
          </Accordion>
        )
      })}
    </Box>
  )
}

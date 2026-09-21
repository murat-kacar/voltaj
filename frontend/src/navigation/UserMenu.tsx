import { useState } from 'react'
import { Avatar, Divider, IconButton, ListItemIcon, ListItemText, Menu, MenuItem } from '@mui/material'
import BarChartIcon from '@mui/icons-material/BarChart'
import Brightness4Icon from '@mui/icons-material/Brightness4'
import Brightness7Icon from '@mui/icons-material/Brightness7'
import LanguageIcon from '@mui/icons-material/Language'
import ListAltIcon from '@mui/icons-material/ListAlt'
import LogoutIcon from '@mui/icons-material/Logout'
import { useI18n, type Language } from '../i18n'

type Props = {
  initials: string
  lang: Language
  mode: 'light' | 'dark'
  onNavigate: (viewId: string) => void
  onToggleLang: () => void
  onToggleMode: () => void
  onLogout: () => void
}

/** Everything that used to sit loose in the top bar, folded under the avatar. */
export function UserMenu({ initials, lang, mode, onNavigate, onToggleLang, onToggleMode, onLogout }: Props) {
  const { translate: t } = useI18n()
  const [anchor, setAnchor] = useState<HTMLElement | null>(null)
  const open = Boolean(anchor)
  const close = () => setAnchor(null)
  const pick = (action: () => void) => () => {
    close()
    action()
  }

  return (
    <>
      <IconButton
        onClick={(event) => setAnchor(event.currentTarget)}
        aria-label={t('common:nav.userMenu')}
        aria-haspopup="menu"
        aria-expanded={open}
        aria-controls={open ? 'user-menu' : undefined}
        data-testid="user-menu-button"
        sx={{ ml: 1 }}
      >
        <Avatar sx={{ width: 32, height: 32, bgcolor: 'primary.main', fontSize: 14 }}>{initials}</Avatar>
      </IconButton>
      <Menu
        id="user-menu"
        anchorEl={anchor}
        open={open}
        onClose={close}
        anchorOrigin={{ vertical: 'bottom', horizontal: 'right' }}
        transformOrigin={{ vertical: 'top', horizontal: 'right' }}
      >
        <MenuItem onClick={pick(() => onNavigate('reports'))} data-testid="user-menu-reports">
          <ListItemIcon><BarChartIcon fontSize="small" /></ListItemIcon>
          <ListItemText>{t('common:nav.reports')}</ListItemText>
        </MenuItem>
        <MenuItem onClick={pick(() => onNavigate('audit-log'))} data-testid="user-menu-audit-log">
          <ListItemIcon><ListAltIcon fontSize="small" /></ListItemIcon>
          <ListItemText>{t('common:nav.auditLog')}</ListItemText>
        </MenuItem>
        <Divider />
        <MenuItem onClick={pick(onToggleLang)} data-testid="user-menu-language">
          <ListItemIcon><LanguageIcon fontSize="small" /></ListItemIcon>
          <ListItemText>{lang === 'en' ? 'Türkçe' : 'English'}</ListItemText>
        </MenuItem>
        <MenuItem onClick={pick(onToggleMode)} data-testid="user-menu-theme">
          <ListItemIcon>{mode === 'dark' ? <Brightness7Icon fontSize="small" /> : <Brightness4Icon fontSize="small" />}</ListItemIcon>
          <ListItemText>{mode === 'dark' ? t('common:nav.lightMode') : t('common:nav.darkMode')}</ListItemText>
        </MenuItem>
        <Divider />
        <MenuItem onClick={pick(onLogout)} data-testid="user-menu-logout">
          <ListItemIcon><LogoutIcon fontSize="small" /></ListItemIcon>
          <ListItemText>{t('common:actions.logout')}</ListItemText>
        </MenuItem>
      </Menu>
    </>
  )
}

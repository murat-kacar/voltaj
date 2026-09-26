import { useState } from 'react'
import { Avatar, Divider, IconButton, ListItemIcon, ListItemText, Menu, MenuItem, Box, ButtonBase, Typography } from '@mui/material'
import Brightness4Icon from '@mui/icons-material/Brightness4'
import Brightness7Icon from '@mui/icons-material/Brightness7'
import LanguageIcon from '@mui/icons-material/Language'
import LogoutIcon from '@mui/icons-material/Logout'
import SettingsIcon from '@mui/icons-material/Settings'
import InfoOutlinedIcon from '@mui/icons-material/InfoOutlined'
import KeyboardArrowDownIcon from '@mui/icons-material/KeyboardArrowDown'
import { useI18n, type Language } from '../i18n'

type Props = {
  initials: string
  name: string
  role: string
  lang: Language
  mode: 'light' | 'dark'
  onNavigate: (viewId: string) => void
  onToggleLang: () => void
  onToggleMode: () => void
  onLogout: () => void
}

/** Everything that used to sit loose in the top bar, folded under the avatar. */
export function UserMenu({ initials, name, role, lang, mode, onNavigate, onToggleLang, onToggleMode, onLogout }: Props) {
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
      <Box 
        sx={{ 
          width: '100%', 
          display: 'flex', 
          alignItems: 'center', 
          justifyContent: 'space-between'
        }}
      >
        <Box 
          component={ButtonBase} 
          onClick={(event: React.MouseEvent<HTMLElement>) => setAnchor(event.currentTarget)}
          aria-label={t('common:nav.userMenu')}
          aria-haspopup="menu"
          aria-expanded={open}
          aria-controls={open ? 'user-menu' : undefined}
          data-testid="user-menu-button"
          sx={{ 
            p: 1, 
            display: 'flex', 
            alignItems: 'center', 
            borderRadius: 2, 
            '&:hover': { bgcolor: 'action.hover' },
            flexGrow: 1,
            justifyContent: 'flex-start',
            gap: 1
          }}
        >
          <Avatar sx={{ width: 32, height: 32, bgcolor: 'primary.main', fontSize: 14, fontWeight: 'bold' }}>{initials}</Avatar>
          <Box sx={{ display: 'flex', alignItems: 'center', gap: 0.5 }}>
            <Typography variant="body2" sx={{ fontWeight: 'bold', color: 'text.primary' }}>{name.split(' ')[0]}</Typography>
            <Typography variant="body2" color="text.secondary">·</Typography>
            <Typography variant="caption" sx={{ color: 'text.secondary', fontWeight: 'bold' }}>{role}</Typography>
            <KeyboardArrowDownIcon fontSize="small" color="action" />
          </Box>
        </Box>
        
        <IconButton size="small" title="Yardım" sx={{ mx: 1 }}>
          <InfoOutlinedIcon fontSize="small" color="action" />
        </IconButton>
      </Box>
      <Menu
        id="user-menu"
        anchorEl={anchor}
        open={open}
        onClose={close}
        anchorOrigin={{ vertical: 'bottom', horizontal: 'right' }}
        transformOrigin={{ vertical: 'top', horizontal: 'right' }}
      >
        <MenuItem onClick={pick(() => onNavigate('settings'))} data-testid="user-menu-settings">
          <ListItemIcon><SettingsIcon fontSize="small" /></ListItemIcon>
          <ListItemText>{t('common:nav.settings')}</ListItemText>
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

import { createTheme, alpha } from '@mui/material/styles';

export const getTheme = (mode: 'light' | 'dark') =>
  createTheme({
    palette: {
      mode,
      primary: {
        main: mode === 'light' ? '#0f172a' : '#f8fafc', // Slate 900 / Slate 50
        light: mode === 'light' ? '#334155' : '#cbd5e1', // Slate 700 / Slate 300
        dark: mode === 'light' ? '#020617' : '#ffffff',  // Slate 950 / White
      },
      secondary: {
        main: '#64748b', // Slate 500
      },
      background: {
        default: mode === 'light' ? '#f8fafc' : '#09090b', // Zinc 50 / Zinc 950
        paper: mode === 'light' ? '#ffffff' : '#18181b',   // White / Zinc 900
      },
      text: {
        primary: mode === 'light' ? '#0f172a' : '#f8fafc',
        secondary: mode === 'light' ? '#64748b' : '#a1a1aa',
      },
      divider: mode === 'light' ? '#e2e8f0' : '#27272a',
    },
    shape: {
      borderRadius: 6,
    },
    typography: {
      fontFamily: '"Inter", "Roboto", "Helvetica", "Arial", sans-serif',
      h1: { fontWeight: 700, letterSpacing: '-0.025em' },
      h2: { fontWeight: 700, letterSpacing: '-0.025em' },
      h3: { fontWeight: 600, letterSpacing: '-0.025em' },
      h4: { fontWeight: 600, letterSpacing: '-0.015em' },
      h5: { fontWeight: 600, letterSpacing: '-0.015em' },
      h6: { fontWeight: 600, letterSpacing: '-0.015em' },
      subtitle1: { fontWeight: 500 },
      subtitle2: { fontWeight: 600 },
      button: { textTransform: 'none', fontWeight: 600 },
    },
    components: {
      MuiButton: {
        styleOverrides: {
          root: {
            boxShadow: 'none',
            '&:hover': {
              boxShadow: 'none',
            },
          },
          containedPrimary: {
            backgroundColor: mode === 'light' ? '#0f172a' : '#f8fafc',
            color: mode === 'light' ? '#ffffff' : '#0f172a',
            '&:hover': {
              backgroundColor: mode === 'light' ? '#334155' : '#e2e8f0',
            },
          },
        },
      },
      MuiPaper: {
        styleOverrides: {
          root: {
            backgroundImage: 'none',
            boxShadow: mode === 'light' 
              ? '0 1px 3px 0 rgb(0 0 0 / 0.1), 0 1px 2px -1px rgb(0 0 0 / 0.1)'
              : '0 4px 6px -1px rgb(0 0 0 / 0.5)',
          },
        },
      },
      MuiAppBar: {
        styleOverrides: {
          root: {
            backgroundColor: mode === 'light' ? '#ffffff' : '#18181b',
            color: mode === 'light' ? '#0f172a' : '#f8fafc',
            borderBottom: `1px solid ${mode === 'light' ? '#e2e8f0' : '#27272a'}`,
          },
        },
      },
      MuiDrawer: {
        styleOverrides: {
          paper: {
            backgroundColor: mode === 'light' ? '#f8fafc' : '#09090b',
            borderRight: `1px solid ${mode === 'light' ? '#e2e8f0' : '#27272a'}`,
          },
        },
      },
    },
  });

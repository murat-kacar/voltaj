import { createTheme } from '@mui/material/styles';

export const getTheme = (mode: 'light' | 'dark') =>
  createTheme({
    palette: {
      mode,
      primary: {
        main: '#e6a351', // Voltflow Amber
      },
      secondary: {
        main: '#2a403d',
      },
      background: {
        default: mode === 'light' ? '#f4f7f6' : '#0a0f0e',
        paper: mode === 'light' ? '#ffffff' : '#131c1a',
      },
    },
    typography: {
      fontFamily: '"Space Grotesk", "Inter", sans-serif',
      h1: { fontWeight: 700 },
      h2: { fontWeight: 700 },
      h3: { fontWeight: 600 },
      h4: { fontWeight: 600 },
      h5: { fontWeight: 600 },
      h6: { fontWeight: 600 },
    },
    components: {
      MuiButton: {
        styleOverrides: {
          root: {
            borderRadius: 8,
            textTransform: 'none',
            fontWeight: 600,
          },
        },
      },
      MuiPaper: {
        styleOverrides: {
          root: {
            backgroundImage: 'none',
          },
        },
      },
    },
  });

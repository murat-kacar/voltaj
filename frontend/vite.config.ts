import react from '@vitejs/plugin-react'
import { defineConfig } from 'vite'

// https://vite.dev/config/
export default defineConfig({
  plugins: [react()],
  server: {
    proxy: {
      '/api': 'http://localhost:5275',
      '/health': 'http://localhost:5275',
      '/ready': 'http://localhost:5275',
      '/metrics': 'http://localhost:5275',
    },
  },
})

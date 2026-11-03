import react from '@vitejs/plugin-react'
import { defineConfig } from 'vite'

// https://vite.dev/config/
export default defineConfig({
  plugins: [react()],
  server: {
    proxy: {
      // Requests to /api/... are forwarded to the backend 
      '/api': 'http://localhost:5101',
    },
  },
})

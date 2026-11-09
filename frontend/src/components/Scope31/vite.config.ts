import react from '@vitejs/plugin-react'
import { defineConfig } from 'vite'

// Forward /api requests from Vite to the local C# backend.
export default defineConfig({
  plugins: [react()],
  server: {
    proxy: {
      '/api': 'http://localhost:5101'
    }
  }
})
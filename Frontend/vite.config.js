import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'
import path from 'path'

// https://vite.dev/config/
export default defineConfig({
  plugins: [react()],
  resolve: {
    alias: {
      '@': path.resolve(__dirname, './src'),
    },
  },
  server: {
    port: 3000,
    proxy: {
      '/api/appointment': {
        target: 'http://localhost:5001',
        changeOrigin: true,
        rewrite: (path) => path.replace(/^\/api\/appointment/, '/api')
      },
      '/api/chat': {
        target: 'http://localhost:5002',
        changeOrigin: true,
        rewrite: (path) => path.replace(/^\/api\/chat/, '/api')
      },
      '/api/friends': {
        target: 'http://localhost:5002',
        changeOrigin: true,
      },
      '/api/notification': {
        target: 'http://localhost:5003',
        changeOrigin: true,
        rewrite: (path) => path.replace(/^\/api\/notification/, '/api')
      },
      '/api/auth': {
        target: 'http://localhost:5005',
        changeOrigin: true,
      },
      '/chathub': {
        target: 'http://localhost:5002',
        changeOrigin: true,
        ws: true
      },
      '/orderhub': {
        target: 'http://localhost:5001',
        changeOrigin: true,
        ws: true
      },
      '/notificationhub': {
        target: 'http://localhost:5003',
        changeOrigin: true,
        ws: true
      },
      '/api/roles': {
        target: 'http://localhost:5005',
        changeOrigin: true,
      },
      '/api/admin': {
        target: 'http://localhost:5005',
        changeOrigin: true,
      }
    }
  }
})

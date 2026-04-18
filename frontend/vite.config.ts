import { fileURLToPath, URL } from 'node:url'
import { defineConfig } from 'vite'
import vue from '@vitejs/plugin-vue'

export default defineConfig({
  plugins: [vue()],
  resolve: {
    alias: {
      '@': fileURLToPath(new URL('./src', import.meta.url))
    }
  },
  server: {
    port: 5173,
    proxy: {
      '/auth': {
        target: 'http://localhost:5070',
        changeOrigin: true,
        secure: false
      },
      '/calls': {
        target: 'http://localhost:5070',
        changeOrigin: true,
        secure: false
      },
      '/health': {
        target: 'http://localhost:5070',
        changeOrigin: true,
        secure: false
      }
    }
  }
})

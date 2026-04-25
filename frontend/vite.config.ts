import { fileURLToPath, URL } from 'node:url'
import { defineConfig } from 'vite'
import vue from '@vitejs/plugin-vue'
import tailwindcss from '@tailwindcss/vite'

export default defineConfig({
  plugins: [vue(), tailwindcss()],
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
      '/admin': {
        target: 'http://localhost:5070',
        changeOrigin: true,
        secure: false
      },
      '/reports': {
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

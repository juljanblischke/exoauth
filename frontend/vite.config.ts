import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'
import tailwindcss from '@tailwindcss/vite'
import path from 'path'

// https://vite.dev/config/
export default defineConfig({
  plugins: [react(), tailwindcss()],
  resolve: {
    alias: {
      '@': path.resolve(__dirname, './src'),
    },
  },
  build: {
    rollupOptions: {
      output: {
        manualChunks: (id) => {
          // React core dependencies
          if (id.includes('node_modules/react') ||
              id.includes('node_modules/react-dom') ||
              id.includes('node_modules/react/jsx-runtime')) {
            return 'react-vendor'
          }

          // TanStack libraries
          if (id.includes('node_modules/@tanstack/react-query') ||
              id.includes('node_modules/@tanstack/react-router') ||
              id.includes('node_modules/@tanstack/react-table')) {
            return 'tanstack-vendor'
          }

          // Radix UI components
          if (id.includes('node_modules/@radix-ui/')) {
            return 'radix-vendor'
          }

          // Tiptap editor
          if (id.includes('node_modules/@tiptap/')) {
            return 'tiptap-vendor'
          }

          // DnD Kit
          if (id.includes('node_modules/@dnd-kit/')) {
            return 'dnd-vendor'
          }

          // UI utilities
          if (id.includes('node_modules/lucide-react') ||
              id.includes('node_modules/date-fns') ||
              id.includes('node_modules/i18next') ||
              id.includes('node_modules/class-variance-authority') ||
              id.includes('node_modules/clsx') ||
              id.includes('node_modules/tailwind-merge')) {
            return 'ui-utils'
          }
        },
        chunkFileNames: 'assets/[name]-[hash].js',
        entryFileNames: 'assets/[name]-[hash].js',
        assetFileNames: 'assets/[name]-[hash].[ext]',
      },
    },
    chunkSizeWarningLimit: 600,
  },
})

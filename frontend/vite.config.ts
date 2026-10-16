import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'

// https://vite.dev/config/
export default defineConfig({
  plugins: [react()],
  server: {
    // The API only allows this origin (Cors:AllowedOrigins), so the port must not drift to 5174.
    port: 5173,
    strictPort: true,
  },
})

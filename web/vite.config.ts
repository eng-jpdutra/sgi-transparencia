import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'

// Configuração do Vite — o servidor de desenvolvimento e empacotador.
// https://vite.dev/config/
export default defineConfig({
  plugins: [react()],
  server: {
    // Porta fixa 5173 (padrão do Vite). É exatamente a origem que o
    // CORS do backend autoriza (appsettings.Development.json). Se mudar
    // aqui, lembre de atualizar lá — as duas pontas precisam combinar.
    port: 5173,
  },
})

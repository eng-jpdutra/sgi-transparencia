import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import { ThemeProvider, CssBaseline } from '@mui/material'
import { QueryClientProvider } from '@tanstack/react-query'

import App from './App.tsx'
import { tema } from './tema/tema.ts'
import { clienteQuery } from './api/clienteQuery.ts'

// Ponto de entrada do frontend. Aqui "embrulhamos" a aplicação nos
// provedores globais — a ORDEM segue o padrão de fora para dentro:
//
//   QueryClientProvider  -> disponibiliza o TanStack Query a tudo
//     ThemeProvider      -> disponibiliza o tema MUI a tudo
//       CssBaseline      -> normaliza o CSS entre navegadores
//         App            -> a aplicação em si
//
// (Nos próximos sub-passos, o provedor de sessão e o roteador
// entrarão nesta mesma pilha.)
createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <QueryClientProvider client={clienteQuery}>
      <ThemeProvider theme={tema}>
        <CssBaseline />
        <App />
      </ThemeProvider>
    </QueryClientProvider>
  </StrictMode>,
)

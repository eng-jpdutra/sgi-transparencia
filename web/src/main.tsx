import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import { ThemeProvider, CssBaseline } from '@mui/material'
import { QueryClientProvider } from '@tanstack/react-query'
import { BrowserRouter } from 'react-router-dom'
import { LocalizationProvider } from '@mui/x-date-pickers/LocalizationProvider'
import { AdapterDateFns } from '@mui/x-date-pickers/AdapterDateFnsV3'
import { ptBR } from 'date-fns/locale'

import App from './App.tsx'
import { tema } from './tema/tema.ts'
import { clienteQuery } from './api/clienteQuery.ts'
import { ProvedorSessao } from './contextos/ProvedorSessao.tsx'

// Ponto de entrada do frontend. Aqui "embrulhamos" a aplicação nos
// provedores globais — a ORDEM segue o padrão de fora para dentro:
//
//   QueryClientProvider  -> disponibiliza o TanStack Query a tudo
//     ThemeProvider      -> disponibiliza o tema MUI a tudo
//       CssBaseline      -> normaliza o CSS entre navegadores
//         LocalizationProvider -> formato/idioma das datas (pt-BR)
//           ProvedorSessao -> disponibiliza o estado de autenticação
//             BrowserRouter -> habilita as rotas por URL
//               App        -> a aplicação (que delega ao roteador)
createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <QueryClientProvider client={clienteQuery}>
      <ThemeProvider theme={tema}>
        <CssBaseline />
        {/* LocalizationProvider: configura idioma/formato das datas
            (pt-BR) para os DatePickers. Legislaturas não usa mais
            seleção de data (cadastro automático), mas mantemos isto
            pronto — mandatos, filiações e ocupações terão datas livres. */}
        <LocalizationProvider
          dateAdapter={AdapterDateFns}
          adapterLocale={ptBR}
        >
          <ProvedorSessao>
            <BrowserRouter>
              <App />
            </BrowserRouter>
          </ProvedorSessao>
        </LocalizationProvider>
      </ThemeProvider>
    </QueryClientProvider>
  </StrictMode>,
)

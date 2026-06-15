import { QueryClient } from '@tanstack/react-query'

// Configuração central do TanStack Query — a camada OBRIGATÓRIA de
// estado de servidor (diretriz v2.1). Toda chamada à API passará por
// useQuery/useMutation, herdando estas políticas de resiliência.
export const clienteQuery = new QueryClient({
  defaultOptions: {
    queries: {
      // RESILIÊNCIA: tenta novamente até 2x em falha transitória de
      // rede, com recuo exponencial (1s, depois 2s). Erros de negócio
      // (4xx) não devem ser repetidos — o tratamento fino disso entra
      // no cliente HTTP central (sub-passo 6.2).
      retry: 2,
      retryDelay: (tentativa) => Math.min(1000 * 2 ** tentativa, 4000),

      // Dados ficam "frescos" por 30s: dentro dessa janela, navegar e
      // voltar para uma tela não dispara nova chamada (economia de
      // rede e resposta instantânea).
      staleTime: 30_000,

      // Não recarrega só porque a janela do navegador reganhou foco —
      // evita rajadas de requisições em quem alterna entre abas.
      refetchOnWindowFocus: false,
    },
  },
})

import { useQuery, useMutation, useQueryClient, keepPreviousData } from '@tanstack/react-query'
import {
  listarLegislaturas, obterProximaLegislatura, criarLegislatura, inativarLegislatura,
  reativarLegislatura,
  type ParametrosListagem,
} from './legislaturas'

/**
 * Hook de listagem — padrão TanStack Query do template canônico.
 * Os parâmetros entram na query key: mudou filtro/página, refaz a
 * busca e cacheia por combinação.
 */
export function usarLegislaturas(params: ParametrosListagem) {
  return useQuery({
    queryKey: ['legislaturas', params],
    queryFn: () => listarLegislaturas(params),
    placeholderData: keepPreviousData,
  })
}

/**
 * Busca o PREVIEW da próxima legislatura sob demanda.
 * enabled:false -> não busca ao montar; a tela dispara via refetch()
 * só quando o usuário clica em "Nova legislatura".
 */
export function usarProximaLegislatura() {
  return useQuery({
    queryKey: ['legislaturas', 'proxima'],
    queryFn: obterProximaLegislatura,
    enabled: false,
    gcTime: 0, // não cacheia: cada clique recalcula do servidor
  })
}

/** Cria a próxima legislatura; ao concluir, atualiza a listagem. */
export function usarCriarLegislatura() {
  const cliente = useQueryClient()
  return useMutation({
    mutationFn: () => criarLegislatura(),
    onSuccess: () => {
      cliente.invalidateQueries({ queryKey: ['legislaturas'] })
    },
  })
}

/** Inativa uma legislatura; ao concluir, atualiza a listagem. */
export function usarInativarLegislatura() {
  const cliente = useQueryClient()
  return useMutation({
    mutationFn: (id: number) => inativarLegislatura(id),
    onSuccess: () => {
      cliente.invalidateQueries({ queryKey: ['legislaturas'] })
    },
  })
}

/** Reativa uma legislatura; ao concluir, atualiza a listagem. */
export function usarReativarLegislatura() {
  const cliente = useQueryClient()
  return useMutation({
    mutationFn: (id: number) => reativarLegislatura(id),
    onSuccess: () => {
      cliente.invalidateQueries({ queryKey: ['legislaturas'] })
    },
  })
}

import { useQuery } from '@tanstack/react-query'
import { requisitar } from './clienteHttp'

// Hooks de leitura para alimentar os SELECTS do formulário de admissão.
// Buscam listas enxutas (primeira página grande) dos cadastros já
// existentes. Cacheados, pois mudam pouco durante o uso.

interface ItemSelect {
  id: number
  rotulo: string
}

interface Paginado<T> { itens: T[] }

/** Cargos ativos para o select. */
export function usarCargosSelect() {
  return useQuery({
    queryKey: ['select', 'cargos'],
    queryFn: async (): Promise<ItemSelect[]> => {
      const r = await requisitar<Paginado<{ id: number; nome: string }>>(
        '/cargos?tamanhoPagina=100&ordenarPor=nome')
      return r.itens.map((c) => ({ id: c.id, rotulo: c.nome }))
    },
    staleTime: 5 * 60 * 1000,
  })
}

/** Regimes ativos para o select. */
export function usarRegimesSelect() {
  return useQuery({
    queryKey: ['select', 'regimes'],
    queryFn: async (): Promise<ItemSelect[]> => {
      const r = await requisitar<Paginado<{ id: number; nome: string }>>(
        '/regimes?tamanhoPagina=100&ordenarPor=nome')
      return r.itens.map((x) => ({ id: x.id, rotulo: x.nome }))
    },
    staleTime: 5 * 60 * 1000,
  })
}

/** Legislaturas para o select (mais recentes primeiro). */
export function usarLegislaturasSelect() {
  return useQuery({
    queryKey: ['select', 'legislaturas'],
    queryFn: async (): Promise<ItemSelect[]> => {
      const r = await requisitar<Paginado<{ id: number; nome: string }>>(
        '/legislaturas?tamanhoPagina=100')
      return r.itens.map((l) => ({ id: l.id, rotulo: l.nome }))
    },
    staleTime: 5 * 60 * 1000,
  })
}

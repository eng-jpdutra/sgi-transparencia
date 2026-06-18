import { useQuery, useMutation, useQueryClient, keepPreviousData } from '@tanstack/react-query'
import {
  listarPartidos, criarPartido, editarPartido, inativarPartido, reativarPartido,
  type ParametrosListagem, type PartidoEntrada,
} from './partidos'

/** Listagem paginada — filtros na query key (refetch automático). */
export function usarPartidos(params: ParametrosListagem) {
  return useQuery({
    queryKey: ['partidos', params],
    queryFn: () => listarPartidos(params),
    placeholderData: keepPreviousData,
  })
}

/** Mutações — ao concluir, invalidam a lista (atualização automática). */
export function usarCriarPartido() {
  const cliente = useQueryClient()
  return useMutation({
    mutationFn: (dados: PartidoEntrada) => criarPartido(dados),
    onSuccess: () => cliente.invalidateQueries({ queryKey: ['partidos'] }),
  })
}

export function usarEditarPartido() {
  const cliente = useQueryClient()
  return useMutation({
    mutationFn: (args: { id: number; dados: PartidoEntrada }) =>
      editarPartido(args.id, args.dados),
    onSuccess: () => cliente.invalidateQueries({ queryKey: ['partidos'] }),
  })
}

export function usarInativarPartido() {
  const cliente = useQueryClient()
  return useMutation({
    mutationFn: (id: number) => inativarPartido(id),
    onSuccess: () => cliente.invalidateQueries({ queryKey: ['partidos'] }),
  })
}

export function usarReativarPartido() {
  const cliente = useQueryClient()
  return useMutation({
    mutationFn: (id: number) => reativarPartido(id),
    onSuccess: () => cliente.invalidateQueries({ queryKey: ['partidos'] }),
  })
}

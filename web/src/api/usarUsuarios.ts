import { useQuery, useMutation, useQueryClient, keepPreviousData } from '@tanstack/react-query'
import {
  listarUsuarios, listarPerfis, criarUsuario, editarUsuario,
  resetarSenha, inativarUsuario, type ParametrosListagem,
} from './usuarios'

/** Listagem paginada de usuários. */
export function usarUsuarios(params: ParametrosListagem) {
  return useQuery({
    queryKey: ['usuarios', params],
    queryFn: () => listarUsuarios(params),
    placeholderData: keepPreviousData,
  })
}

/** Lista de perfis (para o seletor). Cacheada — muda raramente. */
export function usarPerfis() {
  return useQuery({
    queryKey: ['perfis'],
    queryFn: listarPerfis,
    staleTime: 5 * 60 * 1000, // 5 min: perfis quase não mudam
  })
}

/** Mutações — invalidam a listagem ao concluir. */
export function usarCriarUsuario() {
  const cliente = useQueryClient()
  return useMutation({
    mutationFn: (args: { login: string; perfilIds: number[] }) =>
      criarUsuario(args.login, args.perfilIds),
    onSuccess: () => cliente.invalidateQueries({ queryKey: ['usuarios'] }),
  })
}

export function usarEditarUsuario() {
  const cliente = useQueryClient()
  return useMutation({
    mutationFn: (args: { id: number; perfilIds: number[] }) =>
      editarUsuario(args.id, args.perfilIds),
    onSuccess: () => cliente.invalidateQueries({ queryKey: ['usuarios'] }),
  })
}

export function usarResetarSenha() {
  return useMutation({
    mutationFn: (id: number) => resetarSenha(id),
  })
}

export function usarInativarUsuario() {
  const cliente = useQueryClient()
  return useMutation({
    mutationFn: (id: number) => inativarUsuario(id),
    onSuccess: () => cliente.invalidateQueries({ queryKey: ['usuarios'] }),
  })
}

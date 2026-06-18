import { useQuery, useMutation, useQueryClient, keepPreviousData } from '@tanstack/react-query'
import {
  listarPessoas, admitirPessoa, inativarPessoa, buscarPessoaPorCpf,
  type ParametrosListagem,
} from './pessoas'
import type { AdmissaoEntrada } from '../tipos/pessoa'

/** Listagem paginada de pessoas. */
export function usarPessoas(params: ParametrosListagem) {
  return useQuery({
    queryKey: ['pessoas', params],
    queryFn: () => listarPessoas(params),
    placeholderData: keepPreviousData,
  })
}

/**
 * Busca uma pessoa por CPF (habilitada só quando há 11 dígitos).
 * Alimenta o reaproveitamento de cadastro na admissão.
 */
export function usarPessoaPorCpf(cpf: string) {
  const digitos = cpf.replace(/\D/g, '')
  return useQuery({
    queryKey: ['pessoa-por-cpf', digitos],
    queryFn: () => buscarPessoaPorCpf(digitos),
    enabled: digitos.length === 11,
    staleTime: 30 * 1000,
  })
}

/** Admissão unificada — ao concluir, invalida a listagem. */
export function usarAdmitirPessoa() {
  const cliente = useQueryClient()
  return useMutation({
    mutationFn: (dados: AdmissaoEntrada) => admitirPessoa(dados),
    onSuccess: () => cliente.invalidateQueries({ queryKey: ['pessoas'] }),
  })
}

/** Inativar pessoa. */
export function usarInativarPessoa() {
  const cliente = useQueryClient()
  return useMutation({
    mutationFn: (id: number) => inativarPessoa(id),
    onSuccess: () => cliente.invalidateQueries({ queryKey: ['pessoas'] }),
  })
}

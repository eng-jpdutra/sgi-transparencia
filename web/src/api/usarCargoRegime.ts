import { useQuery, useMutation, useQueryClient, keepPreviousData } from '@tanstack/react-query'
import { criarApiNome, type ParametrosListagem } from './cargoRegime'

// Fábrica de hooks para um recurso "nome+ativo". Recebe a chave de
// cache (ex.: 'cargos') e o caminho base, e devolve os hooks prontos.
// Um único código serve a Cargos e Regimes (DRY).
export function criarHooksNome(chave: string, base: string) {
  const api = criarApiNome(base)

  function usarLista(params: ParametrosListagem) {
    return useQuery({
      queryKey: [chave, params],
      queryFn: () => api.listar(params),
      placeholderData: keepPreviousData,
    })
  }

  function usarCriar() {
    const cliente = useQueryClient()
    return useMutation({
      mutationFn: (nome: string) => api.criar(nome),
      onSuccess: () => cliente.invalidateQueries({ queryKey: [chave] }),
    })
  }

  function usarEditar() {
    const cliente = useQueryClient()
    return useMutation({
      mutationFn: (args: { id: number; nome: string }) => api.editar(args.id, args.nome),
      onSuccess: () => cliente.invalidateQueries({ queryKey: [chave] }),
    })
  }

  function usarInativar() {
    const cliente = useQueryClient()
    return useMutation({
      mutationFn: (id: number) => api.inativar(id),
      onSuccess: () => cliente.invalidateQueries({ queryKey: [chave] }),
    })
  }

  return { usarLista, usarCriar, usarEditar, usarInativar }
}

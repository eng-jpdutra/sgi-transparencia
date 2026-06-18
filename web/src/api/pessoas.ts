import { requisitar } from './clienteHttp'
import type {
  Pessoa, AdmissaoEntrada, ResultadoPaginado, FiltrosPessoa,
} from '../tipos/pessoa'

// Funções que falam com /pessoas. Camada fina sobre o cliente HTTP.

export interface ParametrosListagem extends FiltrosPessoa {
  pagina: number
  tamanhoPagina: number
  ordenarPor?: string
  descendente?: boolean
}

/** GET /pessoas — listagem paginada com filtros. */
export function listarPessoas(
  params: ParametrosListagem,
): Promise<ResultadoPaginado<Pessoa>> {
  const qs = new URLSearchParams()
  qs.set('pagina', String(params.pagina))
  qs.set('tamanhoPagina', String(params.tamanhoPagina))
  if (params.busca.trim()) qs.set('busca', params.busca.trim())
  if (params.papel) qs.set('papel', params.papel)
  if (params.ordenarPor) qs.set('ordenarPor', params.ordenarPor)
  if (params.descendente) qs.set('descendente', 'true')
  if (params.incluirInativos) qs.set('incluirInativos', 'true')

  return requisitar<ResultadoPaginado<Pessoa>>(`/pessoas?${qs.toString()}`)
}

/** POST /pessoas/admissao — cadastro unificado (pessoa + ficha + exercício). */
export function admitirPessoa(dados: AdmissaoEntrada): Promise<Pessoa> {
  return requisitar<Pessoa>('/pessoas/admissao', { metodo: 'POST', corpo: dados })
}

/**
 * Procura uma pessoa pelo CPF (exato). Usa a própria listagem (a busca
 * server-side já cobre CPF) e filtra o match exato. Inclui inativas para
 * que a admissão possa avisar "reative antes". Retorna null se não houver.
 */
export async function buscarPessoaPorCpf(cpf: string): Promise<Pessoa | null> {
  const digitos = cpf.replace(/\D/g, '')
  if (digitos.length !== 11) return null
  const qs = new URLSearchParams({
    busca: digitos, tamanhoPagina: '5', incluirInativos: 'true',
  })
  const r = await requisitar<ResultadoPaginado<Pessoa>>(`/pessoas?${qs.toString()}`)
  return r.itens.find((p) => p.cpf === digitos) ?? null
}

/** DELETE /pessoas/{id} — inativar (soft delete). */
export function inativarPessoa(id: number): Promise<void> {
  return requisitar<void>(`/pessoas/${id}`, { metodo: 'DELETE' })
}

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

/** DELETE /pessoas/{id} — inativar (soft delete). */
export function inativarPessoa(id: number): Promise<void> {
  return requisitar<void>(`/pessoas/${id}`, { metodo: 'DELETE' })
}

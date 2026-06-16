import { requisitar } from './clienteHttp'
import type { Partido, ResultadoPaginado, FiltrosPartido } from '../tipos/partido'

// Funções que falam com /partidos. Camada fina sobre o cliente HTTP.

export interface ParametrosListagem extends FiltrosPartido {
  pagina: number
  tamanhoPagina: number
  ordenarPor?: string
  descendente?: boolean
}

/** GET /partidos — listagem paginada com filtros. */
export function listarPartidos(
  params: ParametrosListagem,
): Promise<ResultadoPaginado<Partido>> {
  const qs = new URLSearchParams()
  qs.set('pagina', String(params.pagina))
  qs.set('tamanhoPagina', String(params.tamanhoPagina))
  if (params.busca.trim()) qs.set('busca', params.busca.trim())
  if (params.ordenarPor) qs.set('ordenarPor', params.ordenarPor)
  if (params.descendente) qs.set('descendente', 'true')
  if (params.incluirInativos) qs.set('incluirInativos', 'true')

  return requisitar<ResultadoPaginado<Partido>>(`/partidos?${qs.toString()}`)
}

/** Dados enviados ao criar/editar. */
export interface PartidoEntrada {
  sigla: string
  nome: string
  numero: number
}

/** POST /partidos — criar. */
export function criarPartido(dados: PartidoEntrada): Promise<Partido> {
  return requisitar<Partido>('/partidos', { metodo: 'POST', corpo: dados })
}

/** PUT /partidos/{id} — editar. */
export function editarPartido(id: number, dados: PartidoEntrada): Promise<Partido> {
  return requisitar<Partido>(`/partidos/${id}`, { metodo: 'PUT', corpo: dados })
}

/** DELETE /partidos/{id} — inativar (soft delete). */
export function inativarPartido(id: number): Promise<void> {
  return requisitar<void>(`/partidos/${id}`, { metodo: 'DELETE' })
}

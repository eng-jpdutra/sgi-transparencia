import { requisitar } from './clienteHttp'
import type {
  Legislatura, ResultadoPaginado, FiltrosLegislatura,
} from '../tipos/legislatura'

// Funções que falam com os endpoints de /legislaturas. Camada fina:
// montam URL/corpo e delegam ao cliente HTTP central.

/** Parâmetros da listagem paginada. */
export interface ParametrosListagem extends FiltrosLegislatura {
  pagina: number
  tamanhoPagina: number
  ordenarPor?: string
  descendente?: boolean
}

/** GET /legislaturas — listagem paginada com filtros. */
export function listarLegislaturas(
  params: ParametrosListagem,
): Promise<ResultadoPaginado<Legislatura>> {
  const busca = new URLSearchParams()
  busca.set('pagina', String(params.pagina))
  busca.set('tamanhoPagina', String(params.tamanhoPagina))
  const ano = params.ano.trim()
  if (ano && /^\d+$/.test(ano)) busca.set('ano', ano)
  if (params.ordenarPor) busca.set('ordenarPor', params.ordenarPor)
  if (params.descendente) busca.set('descendente', 'true')
  if (params.incluirInativos) busca.set('incluirInativos', 'true')

  return requisitar<ResultadoPaginado<Legislatura>>(
    `/legislaturas?${busca.toString()}`,
  )
}

/** Preview da próxima legislatura (o que SERÁ criado). */
export interface ProximaLegislatura {
  numero: number
  anoInicio: number
  anoFim: number
  nome: string
  dataInicio: string
  dataFim: string
}

/** GET /legislaturas/proxima — calcula sem gravar. */
export function obterProximaLegislatura(): Promise<ProximaLegislatura> {
  return requisitar<ProximaLegislatura>('/legislaturas/proxima')
}

/** POST /legislaturas — cria a próxima (sem corpo: o backend calcula). */
export function criarLegislatura(): Promise<Legislatura> {
  return requisitar<Legislatura>('/legislaturas', { metodo: 'POST' })
}

/** DELETE /legislaturas/{id} — inativar (soft delete). */
export function inativarLegislatura(id: number): Promise<void> {
  return requisitar<void>(`/legislaturas/${id}`, { metodo: 'DELETE' })
}

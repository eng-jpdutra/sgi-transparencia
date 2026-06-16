import { requisitar } from './clienteHttp'
import type { ResultadoPaginado, FiltrosNome } from '../tipos/cargoRegime'

// Como Cargos e Regimes têm a MESMA forma (nome + ativo), uma única
// fábrica de funções de API serve aos dois — basta o caminho base
// (/cargos ou /regimes). DRY de verdade: a lógica mora num lugar só.

export interface ParametrosListagem extends FiltrosNome {
  pagina: number
  tamanhoPagina: number
  ordenarPor?: string
  descendente?: boolean
}

/** Entidade genérica com nome + ativo. */
export interface ItemNome {
  id: number
  nome: string
  ativo: boolean
}

/** Cria um conjunto de funções de API para um recurso "nome+ativo". */
export function criarApiNome(base: string) {
  return {
    listar(params: ParametrosListagem): Promise<ResultadoPaginado<ItemNome>> {
      const qs = new URLSearchParams()
      qs.set('pagina', String(params.pagina))
      qs.set('tamanhoPagina', String(params.tamanhoPagina))
      if (params.busca.trim()) qs.set('busca', params.busca.trim())
      if (params.ordenarPor) qs.set('ordenarPor', params.ordenarPor)
      if (params.descendente) qs.set('descendente', 'true')
      if (params.incluirInativos) qs.set('incluirInativos', 'true')
      return requisitar<ResultadoPaginado<ItemNome>>(`${base}?${qs.toString()}`)
    },
    criar(nome: string): Promise<ItemNome> {
      return requisitar<ItemNome>(base, { metodo: 'POST', corpo: { nome } })
    },
    editar(id: number, nome: string): Promise<ItemNome> {
      return requisitar<ItemNome>(`${base}/${id}`, { metodo: 'PUT', corpo: { nome } })
    },
    inativar(id: number): Promise<void> {
      return requisitar<void>(`${base}/${id}`, { metodo: 'DELETE' })
    },
  }
}

export const apiCargos = criarApiNome('/cargos')
export const apiRegimes = criarApiNome('/regimes')

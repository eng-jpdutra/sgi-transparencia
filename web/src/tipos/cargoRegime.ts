// Tipos dos domínios Cargos e Regimes — estrutura idêntica
// (nome + ativo). Espelham os DTOs do backend.

export interface Cargo {
  id: number
  nome: string
  ativo: boolean
}

export interface Regime {
  id: number
  nome: string
  ativo: boolean
}

/** Envelope genérico de listagem. */
export interface ResultadoPaginado<T> {
  itens: T[]
  totalRegistros: number
  pagina: number
  tamanhoPagina: number
}

/** Filtros (comum aos dois: busca por nome + incluir inativos). */
export interface FiltrosNome {
  busca: string
  incluirInativos: boolean
}

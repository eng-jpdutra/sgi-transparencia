// Tipos do domínio Pessoas — espelham os DTOs do backend.

export interface Papel {
  id: number
  ativo: boolean
}

export interface PapelVereador {
  id: number
  ativo: boolean
  nomeLegislativo: string
}

/** Espelha PessoaSaida.cs — papéis vêm nulos quando não existem. */
export interface Pessoa {
  id: number
  nomeCompleto: string
  matricula: string
  ativo: boolean
  servidor: Papel | null
  vereador: PapelVereador | null
}

/** Envelope genérico de listagem. */
export interface ResultadoPaginado<T> {
  itens: T[]
  totalRegistros: number
  pagina: number
  tamanhoPagina: number
}

/** Filtros da pesquisa de pessoas. */
export interface FiltrosPessoa {
  busca: string
  papel: '' | 'servidor' | 'vereador'
  incluirInativos: boolean
}

/** Dados da ADMISSÃO unificada (espelha AdmissaoEntrada.cs). */
export interface AdmissaoEntrada {
  nomeCompleto: string
  matricula: string
  tipo: 'servidor' | 'vereador'
  nomeLegislativo?: string
  cargoId?: number
  regimeId?: number
  legislaturaId?: number
  dataInicio: string       // 'YYYY-MM-DD'
  dataFim?: string | null
}

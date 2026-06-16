// Tipos do domínio Partidos — espelham os DTOs do backend.

/** Espelha PartidoSaida.cs */
export interface Partido {
  id: number
  sigla: string
  nome: string
  numero: number
  ativo: boolean
}

/** Envelope genérico de listagem (espelha ResultadoPaginado<T>). */
export interface ResultadoPaginado<T> {
  itens: T[]
  totalRegistros: number
  pagina: number
  tamanhoPagina: number
}

/** Filtros da tela de pesquisa (lista FECHADA). */
export interface FiltrosPartido {
  busca: string // casa sigla OU nome
  incluirInativos: boolean
}

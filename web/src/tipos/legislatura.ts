// Tipos do domínio Legislaturas — espelham os DTOs do backend.

/** Espelha LegislaturaSaida.cs (com os derivados já calculados). */
export interface Legislatura {
  id: number
  numero: number
  anoInicio: number
  anoFim: number
  nome: string       // "22ª LEGISLATURA (2025 – 2028)" — pronto p/ exibir
  dataInicio: string // 'YYYY-MM-DD'
  dataFim: string
  ativo: boolean
}

/** Envelope genérico de toda listagem (espelha ResultadoPaginado<T>). */
export interface ResultadoPaginado<T> {
  itens: T[]
  totalRegistros: number
  pagina: number
  tamanhoPagina: number
}

/** Filtros da tela de pesquisa (lista FECHADA). */
export interface FiltrosLegislatura {
  ano: string // texto no input; convertido para número ao enviar
  incluirInativos: boolean
}

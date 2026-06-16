// Tipos do domínio Usuários — espelham os DTOs do backend.

export interface Perfil {
  id: number
  nome: string
  descricao: string | null
}

/** Espelha UsuarioSaida.cs — NUNCA traz senha/hash. */
export interface Usuario {
  id: number
  login: string
  deveTrocarSenha: boolean
  bloqueado: boolean
  ativo: boolean
  perfis: Perfil[]
}

/** Envelope genérico de listagem. */
export interface ResultadoPaginado<T> {
  itens: T[]
  totalRegistros: number
  pagina: number
  tamanhoPagina: number
}

/** Filtros da pesquisa de usuários. */
export interface FiltrosUsuario {
  busca: string // por login
  incluirInativos: boolean
}

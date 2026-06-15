// Tipos que espelham os contratos (DTOs) do backend. Mantê-los
// alinhados com o C# é o que dá tipagem forte de ponta a ponta:
// se o backend mudar um contrato, é aqui que atualizamos, e o
// compilador aponta cada ponto do front afetado.

/** Espelha RespostaLogin.cs (Etapa 4.1: sem o refresh token). */
export interface RespostaLogin {
  tokenAcesso: string
  expiraEmUtc: string // ISO 8601; vira Date quando necessário
  deveTrocarSenha: boolean
}

/** Identidade do usuário logado — resposta de GET /autenticacao/eu. */
export interface UsuarioLogado {
  id: string
  login: string
  perfis: string[]
}

/** Formato padronizado de erro vindo da API (mensagem amigável). */
export interface ErroApi {
  mensagem?: string
  title?: string // ProblemDetails (erros 500)
}

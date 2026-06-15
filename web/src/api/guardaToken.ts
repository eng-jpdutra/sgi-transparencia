// Guarda do ACCESS TOKEN — exclusivamente EM MEMÓRIA (uma variável
// neste módulo). Decisão de segurança (sub-passo 6.1):
//   - Não usamos localStorage nem sessionStorage: ambos são legíveis
//     por qualquer script, logo vulneráveis a roubo via XSS.
//   - Em memória, o token some ao fechar/recarregar a aba — e isso é
//     bom: a sessão é reconstruída pelo cookie HttpOnly via /renovar.
//
// É um módulo simples (sem React) de propósito: o cliente HTTP, que
// também não é React, precisa ler o token sem depender de hooks.

let tokenAcessoAtual: string | null = null

export const guardaToken = {
  obter: (): string | null => tokenAcessoAtual,
  definir: (token: string | null): void => {
    tokenAcessoAtual = token
  },
  limpar: (): void => {
    tokenAcessoAtual = null
  },
}

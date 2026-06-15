import type { UsuarioLogado } from './autenticacao'

// Tipos do contexto de sessão — o "estado de quem está logado" que
// fica disponível para toda a aplicação via o hook useSessao().

/** Em que ponto do ciclo de vida a sessão está. */
export type SituacaoSessao =
  | 'carregando'      // reconstrução inicial em andamento (não pisca a tela)
  | 'autenticado'     // há usuário logado
  | 'naoAutenticado'  // ninguém logado (mostra o login)

/** O que o contexto expõe a qualquer componente. */
export interface ContextoSessao {
  situacao: SituacaoSessao
  usuario: UsuarioLogado | null

  /** true enquanto a senha provisória não foi trocada. */
  deveTrocarSenha: boolean

  /** Efetua login; lança ErroRequisicao em falha (tratado na tela). */
  entrar: (login: string, senha: string) => Promise<void>

  /** Encerra a sessão (revoga no servidor e limpa o estado local). */
  sair: () => Promise<void>

  /** Marca a troca de senha como concluída (chamado após sucesso). */
  concluirTrocaSenha: () => void
}

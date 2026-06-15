import {
  createContext, useCallback, useEffect, useState, type ReactNode,
} from 'react'
import { requisitar } from '../api/clienteHttp'
import { guardaToken } from '../api/guardaToken'
import type { RespostaLogin, UsuarioLogado } from '../tipos/autenticacao'
import type { ContextoSessao, SituacaoSessao } from '../tipos/sessao'

// O contexto em si. Começa indefinido; o hook useSessao() (arquivo
// separado) garante que só seja consumido dentro do provedor.
export const Sessao = createContext<ContextoSessao | undefined>(undefined)

/**
 * ProvedorSessao — a fonte ÚNICA de verdade sobre autenticação.
 * Envolve a aplicação inteira (ver main.tsx) e expõe, via contexto:
 * quem está logado, em que situação, e as ações entrar/sair.
 */
export function ProvedorSessao({ children }: { children: ReactNode }) {
  const [situacao, setSituacao] = useState<SituacaoSessao>('carregando')
  const [usuario, setUsuario] = useState<UsuarioLogado | null>(null)
  const [deveTrocarSenha, setDeveTrocarSenha] = useState(false)

  // -------------------------------------------------------------------
  // Carrega a identidade do usuário logado (GET /eu) e marca a sessão
  // como autenticada. Usado tanto após o login quanto na reconstrução.
  // -------------------------------------------------------------------
  const carregarUsuario = useCallback(async () => {
    const eu = await requisitar<UsuarioLogado>('/autenticacao/eu')
    setUsuario(eu)
    setSituacao('autenticado')
  }, [])

  // -------------------------------------------------------------------
  // RECONSTRUÇÃO DE SESSÃO (roda UMA vez, ao abrir/recarregar o app).
  // O token de acesso vive em memória e morre no F5; então tentamos
  // renovar a partir do cookie HttpOnly. Se der certo, o usuário entra
  // direto, sem ver a tela de login. É o "F5 que travava" do 6.2
  // virando entrada automática e elegante.
  // -------------------------------------------------------------------
  useEffect(() => {
    let cancelado = false

    async function reconstruir() {
      try {
        // Pede um access token novo usando o cookie de sessão.
        const resposta = await fetch(
          `${import.meta.env.VITE_API_URL}/autenticacao/renovar`,
          { method: 'POST', credentials: 'include' },
        )

        if (cancelado) return

        if (!resposta.ok) {
          // Sem sessão válida: cai na tela de login (situação normal).
          setSituacao('naoAutenticado')
          return
        }

        const dados: RespostaLogin = await resposta.json()
        guardaToken.definir(dados.tokenAcesso)
        setDeveTrocarSenha(dados.deveTrocarSenha)
        await carregarUsuario()
      } catch {
        if (!cancelado) setSituacao('naoAutenticado')
      }
    }

    reconstruir()

    // Limpeza: se o componente desmontar no meio (StrictMode roda o
    // efeito duas vezes em DEV), evitamos atualizar estado órfão.
    return () => { cancelado = true }
  }, [carregarUsuario])

  // -------------------------------------------------------------------
  // entrar — login explícito pelo formulário.
  // -------------------------------------------------------------------
  const entrar = useCallback(async (login: string, senha: string) => {
    const dados = await requisitar<RespostaLogin>('/autenticacao/login', {
      metodo: 'POST',
      corpo: { login, senha },
    })
    guardaToken.definir(dados.tokenAcesso)
    setDeveTrocarSenha(dados.deveTrocarSenha)
    await carregarUsuario()
  }, [carregarUsuario])

  // -------------------------------------------------------------------
  // sair — encerra a sessão no servidor e zera o estado local.
  // -------------------------------------------------------------------
  const sair = useCallback(async () => {
    try {
      await requisitar<void>('/autenticacao/sair', { metodo: 'POST' })
    } finally {
      // Independe do resultado do servidor: localmente, saímos sempre.
      guardaToken.limpar()
      setUsuario(null)
      setDeveTrocarSenha(false)
      setSituacao('naoAutenticado')
    }
  }, [])

  const concluirTrocaSenha = useCallback(() => {
    setDeveTrocarSenha(false)
  }, [])

  return (
    <Sessao.Provider
      value={{
        situacao, usuario, deveTrocarSenha,
        entrar, sair, concluirTrocaSenha,
      }}
    >
      {children}
    </Sessao.Provider>
  )
}

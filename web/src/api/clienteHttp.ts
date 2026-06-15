import { guardaToken } from './guardaToken'
import type { ErroApi, RespostaLogin } from '../tipos/autenticacao'

// =====================================================================
// CLIENTE HTTP CENTRAL — a ÚNICA porta de saída do frontend para a API.
// Nenhum componente faz fetch direto; todos passam por aqui. Isso
// concentra num só lugar: anexar o token, renovar quando expira,
// padronizar erros e enviar o cookie de sessão.
// =====================================================================

const URL_BASE = import.meta.env.VITE_API_URL

/** Erro de aplicação com a mensagem JÁ amigável para exibir ao usuário. */
export class ErroRequisicao extends Error {
  constructor(
    public readonly status: number,
    mensagem: string,
  ) {
    super(mensagem)
    this.name = 'ErroRequisicao'
  }
}

// ---------------------------------------------------------------------
// Controle de renovação concorrente (a "fila de espera").
// Se várias requisições tomarem 401 ao mesmo tempo, NÃO disparamos
// várias renovações: a primeira renova, as demais aguardam a MESMA
// promessa e seguem com o token novo.
// ---------------------------------------------------------------------
let renovacaoEmAndamento: Promise<boolean> | null = null

async function renovarSessao(): Promise<boolean> {
  // Já há uma renovação rodando? Aguarda ela em vez de abrir outra.
  if (renovacaoEmAndamento) return renovacaoEmAndamento

  renovacaoEmAndamento = (async () => {
    try {
      const resposta = await fetch(`${URL_BASE}/autenticacao/renovar`, {
        method: 'POST',
        // credentials:'include' = ENVIA o cookie HttpOnly do refresh
        // token. É o que permite a renovação sem o JS ver o cookie.
        credentials: 'include',
      })

      if (!resposta.ok) {
        guardaToken.limpar()
        return false
      }

      const dados: RespostaLogin = await resposta.json()
      guardaToken.definir(dados.tokenAcesso)
      return true
    } catch {
      guardaToken.limpar()
      return false
    } finally {
      // Libera para futuras renovações.
      renovacaoEmAndamento = null
    }
  })()

  return renovacaoEmAndamento
}

// ---------------------------------------------------------------------
// Tradução de status HTTP em mensagem amigável (diretriz: nunca expor
// detalhe técnico ao usuário). Tenta primeiro a mensagem que a própria
// API mandou; cai num texto genérico por status só se faltar.
// ---------------------------------------------------------------------
async function extrairMensagemErro(resposta: Response): Promise<string> {
  try {
    const corpo: ErroApi = await resposta.json()
    if (corpo.mensagem) return corpo.mensagem
    if (corpo.title) return corpo.title
  } catch {
    // corpo vazio ou não-JSON: usa o genérico abaixo
  }

  switch (resposta.status) {
    case 400: return 'Dados inválidos. Verifique e tente novamente.'
    case 401: return 'Sessão expirada. Entre novamente.'
    case 403: return 'Você não tem permissão para esta ação.'
    case 404: return 'Recurso não encontrado.'
    case 429: return 'Muitas tentativas. Aguarde um instante.'
    default:  return 'Ocorreu um erro inesperado. Tente novamente.'
  }
}

// ---------------------------------------------------------------------
// A função central. Todo acesso à API passa por requisitar<T>().
// ---------------------------------------------------------------------
interface OpcoesRequisicao {
  metodo?: 'GET' | 'POST' | 'PUT' | 'DELETE'
  corpo?: unknown
  /** Interno: marca a re-tentativa pós-renovação (evita loop infinito). */
  _jaRenovou?: boolean
}

export async function requisitar<T>(
  caminho: string,
  opcoes: OpcoesRequisicao = {},
): Promise<T> {
  const { metodo = 'GET', corpo, _jaRenovou = false } = opcoes

  const cabecalhos: Record<string, string> = {}

  // Anexa o access token (se houver) — é o "Bearer" das rotas protegidas.
  const token = guardaToken.obter()
  if (token) cabecalhos['Authorization'] = `Bearer ${token}`

  if (corpo !== undefined) cabecalhos['Content-Type'] = 'application/json'

  const resposta = await fetch(`${URL_BASE}${caminho}`, {
    method: metodo,
    headers: cabecalhos,
    body: corpo !== undefined ? JSON.stringify(corpo) : undefined,
    // Envia o cookie de sessão também aqui (necessário p/ /sair).
    credentials: 'include',
  })

  // ----- RENOVAÇÃO AUTOMÁTICA -----
  // 401 + ainda não tentamos renovar + não é a própria rota de auth:
  // tenta renovar a sessão UMA vez e repete a requisição original.
  // O usuário não percebe nada — a Etapa 4 trabalhando nos bastidores.
  if (
    resposta.status === 401 &&
    !_jaRenovou &&
    !caminho.startsWith('/autenticacao/')
  ) {
    const renovou = await renovarSessao()
    if (renovou) {
      return requisitar<T>(caminho, { ...opcoes, _jaRenovou: true })
    }
  }

  if (!resposta.ok) {
    throw new ErroRequisicao(
      resposta.status,
      await extrairMensagemErro(resposta),
    )
  }

  // 204 No Content (ex.: logout, troca de senha) não tem corpo.
  if (resposta.status === 204) return undefined as T

  return resposta.json() as Promise<T>
}

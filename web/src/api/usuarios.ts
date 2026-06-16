import { requisitar } from './clienteHttp'
import type {
  Usuario, Perfil, ResultadoPaginado, FiltrosUsuario,
} from '../tipos/usuario'

// Funções que falam com /usuarios e /perfis. Camada fina sobre o
// cliente HTTP central.

export interface ParametrosListagem extends FiltrosUsuario {
  pagina: number
  tamanhoPagina: number
  ordenarPor?: string
  descendente?: boolean
}

/** GET /usuarios — listagem paginada. */
export function listarUsuarios(
  params: ParametrosListagem,
): Promise<ResultadoPaginado<Usuario>> {
  const qs = new URLSearchParams()
  qs.set('pagina', String(params.pagina))
  qs.set('tamanhoPagina', String(params.tamanhoPagina))
  if (params.busca.trim()) qs.set('busca', params.busca.trim())
  if (params.ordenarPor) qs.set('ordenarPor', params.ordenarPor)
  if (params.descendente) qs.set('descendente', 'true')
  if (params.incluirInativos) qs.set('incluirInativos', 'true')

  return requisitar<ResultadoPaginado<Usuario>>(`/usuarios?${qs.toString()}`)
}

/** GET /perfis — lista de perfis para o seletor. */
export function listarPerfis(): Promise<Perfil[]> {
  return requisitar<Perfil[]>('/perfis')
}

/** Resposta da criação: usuário + senha provisória (uma única vez). */
export interface UsuarioCriado {
  usuario: Usuario
  senhaProvisoria: string
}

/** POST /usuarios — criar (o backend gera a senha provisória). */
export function criarUsuario(
  login: string, perfilIds: number[],
): Promise<UsuarioCriado> {
  return requisitar<UsuarioCriado>('/usuarios', {
    metodo: 'POST',
    corpo: { login, perfilIds },
  })
}

/** PUT /usuarios/{id} — editar perfis. */
export function editarUsuario(
  id: number, perfilIds: number[],
): Promise<Usuario> {
  return requisitar<Usuario>(`/usuarios/${id}`, {
    metodo: 'PUT',
    corpo: { perfilIds },
  })
}

/** POST /usuarios/{id}/resetar-senha — nova senha provisória. */
export function resetarSenha(id: number): Promise<{ senhaProvisoria: string }> {
  return requisitar<{ senhaProvisoria: string }>(
    `/usuarios/${id}/resetar-senha`, { metodo: 'POST' },
  )
}

/** DELETE /usuarios/{id} — inativar (soft delete). */
export function inativarUsuario(id: number): Promise<void> {
  return requisitar<void>(`/usuarios/${id}`, { metodo: 'DELETE' })
}

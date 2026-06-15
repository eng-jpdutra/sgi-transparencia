import { Routes, Route, Navigate } from 'react-router-dom'
import { useSessao } from '../contextos/useSessao'
import { GuardaAutenticacao } from './GuardaAutenticacao'
import { GuardaPerfil } from './GuardaPerfil'
import { LayoutAutenticado } from '../componentes/LayoutAutenticado'
import { PaginaLogin } from '../paginas/PaginaLogin'
import { PaginaTrocaSenha } from '../paginas/PaginaTrocaSenha'
import { PaginaInicio } from '../paginas/PaginaInicio'
import { PaginaAdmin } from '../paginas/PaginaAdmin'
import { Pagina404 } from '../paginas/Pagina404'

/**
 * Mapa de rotas da aplicação — a fonte única de verdade da navegação.
 *
 * Hierarquia (de fora para dentro):
 *   /login          público; se já logado, vai para o início
 *   /trocar-senha   logado, mas tela "cheia" (sem moldura)
 *   /               GuardaAutenticacao -> LayoutAutenticado (moldura)
 *      /            PaginaInicio
 *      /admin       GuardaPerfil("Admin") -> PaginaAdmin
 *   *               Pagina404
 *
 * Os guardas são rotas-PAI: validam e, se liberam, renderizam a rota
 * filha pelo <Outlet />. É o padrão de rotas aninhadas do Router v6.
 */
export function Rotas() {
  const { situacao } = useSessao()

  return (
    <Routes>
      {/* Login: se o usuário já está autenticado, não faz sentido ver
          o login — redireciona ao início. */}
      <Route
        path="/login"
        element={
          situacao === 'autenticado'
            ? <Navigate to="/" replace />
            : <PaginaLogin />
        }
      />

      {/* Troca de senha: tela cheia, fora da moldura autenticada. */}
      <Route path="/trocar-senha" element={<PaginaTrocaSenha />} />

      {/* Tudo abaixo exige autenticação (GuardaAutenticacao) e vive
          dentro da moldura (LayoutAutenticado). */}
      <Route element={<GuardaAutenticacao />}>
        <Route element={<LayoutAutenticado />}>
          <Route path="/" element={<PaginaInicio />} />

          {/* Sub-árvore que ainda exige o perfil Admin (GuardaPerfil). */}
          <Route element={<GuardaPerfil perfis={['Admin']} />}>
            <Route path="/admin" element={<PaginaAdmin />} />
          </Route>
        </Route>
      </Route>

      {/* Qualquer outra URL: 404. */}
      <Route path="*" element={<Pagina404 />} />
    </Routes>
  )
}

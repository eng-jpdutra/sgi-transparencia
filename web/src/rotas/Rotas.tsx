import { Routes, Route, Navigate } from 'react-router-dom'
import { useSessao } from '../contextos/useSessao'
import { GuardaAutenticacao } from './GuardaAutenticacao'
import { GuardaPerfil } from './GuardaPerfil'
import { LayoutAutenticado } from '../componentes/LayoutAutenticado'
import { PaginaLogin } from '../paginas/PaginaLogin'
import { PaginaTrocaSenha } from '../paginas/PaginaTrocaSenha'
import { PaginaInicio } from '../paginas/PaginaInicio'
import { PaginaLegislaturas } from '../paginas/PaginaLegislaturas'
import { PaginaPartidos } from '../paginas/PaginaPartidos'
import { PaginaCargos } from '../paginas/PaginaCargos'
import { PaginaRegimes } from '../paginas/PaginaRegimes'
import { PaginaPessoas } from '../paginas/PaginaPessoas'
import { PaginaUsuarios } from '../paginas/PaginaUsuarios'
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

      {/* Troca de senha: tela cheia, fora da moldura autenticada.
          Só faz sentido para quem está autenticado COM senha provisória
          pendente. Quando o sair() (após a troca) muda a situação para
          naoAutenticado, esta rota redireciona ao login — é o que
          desbloqueia a tela após a troca bem-sucedida. */}
      <Route
        path="/trocar-senha"
        element={
          situacao !== 'autenticado'
            ? <Navigate to="/login" replace />
            : <PaginaTrocaSenha />
        }
      />

      {/* Tudo abaixo exige autenticação (GuardaAutenticacao) e vive
          dentro da moldura (LayoutAutenticado). */}
      <Route element={<GuardaAutenticacao />}>
        <Route element={<LayoutAutenticado />}>
          <Route path="/" element={<PaginaInicio />} />
          <Route path="/legislaturas" element={<PaginaLegislaturas />} />
          <Route path="/partidos" element={<PaginaPartidos />} />
          <Route path="/cargos" element={<PaginaCargos />} />
          <Route path="/regimes" element={<PaginaRegimes />} />
          <Route path="/pessoas" element={<PaginaPessoas />} />

          {/* Sub-árvore que exige o perfil Admin (GuardaPerfil). */}
          <Route element={<GuardaPerfil perfis={['Admin']} />}>
            <Route path="/usuarios" element={<PaginaUsuarios />} />
            <Route path="/admin" element={<PaginaAdmin />} />
          </Route>
        </Route>
      </Route>

      {/* Qualquer outra URL: 404. */}
      <Route path="*" element={<Pagina404 />} />
    </Routes>
  )
}

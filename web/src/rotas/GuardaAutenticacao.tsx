import { Navigate, Outlet } from 'react-router-dom'
import { useSessao } from '../contextos/useSessao'
import { TelaCarregando } from '../componentes/TelaCarregando'

/**
 * GuardaAutenticacao — protege um conjunto de rotas exigindo que haja
 * usuário logado. Usado como rota "pai": as rotas filhas só renderizam
 * se este guarda liberar (via <Outlet />).
 *
 * AVISO DE GOVERNANÇA (importante): este guarda é CONVENIÊNCIA DE UX,
 * não segurança. Ele só evita que o usuário navegue para telas que
 * o backend recusaria. A segurança REAL é o [Authorize] do backend,
 * que valida o JWT a cada requisição — e que já testamos. Esconder
 * uma rota no front não protege dado nenhum; protegê-la no back, sim.
 */
export function GuardaAutenticacao() {
  const { situacao, deveTrocarSenha } = useSessao()

  // Ainda reconstruindo a sessão? Não decide nada — espera.
  if (situacao === 'carregando') {
    return <TelaCarregando mensagem="Verificando sua sessão…" />
  }

  // Não logado: manda para o login.
  if (situacao === 'naoAutenticado') {
    return <Navigate to="/login" replace />
  }

  // Logado, mas com senha provisória pendente: bloqueia tudo e
  // força a troca de senha antes de liberar qualquer rota interna.
  if (deveTrocarSenha) {
    return <Navigate to="/trocar-senha" replace />
  }

  // Liberado: renderiza a rota filha solicitada.
  return <Outlet />
}

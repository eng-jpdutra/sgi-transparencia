import { criarHooksNome } from '../api/usarCargoRegime'
import { PaginaNome } from './PaginaNome'

// Instância concreta da página genérica para Regimes de Contratação.
const hooksRegimes = criarHooksNome('regimes', '/regimes')

export function PaginaRegimes() {
  return <PaginaNome titulo="Regimes de Contratação" rotulo="regime" hooks={hooksRegimes} />
}

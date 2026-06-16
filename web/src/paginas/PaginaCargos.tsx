import { criarHooksNome } from '../api/usarCargoRegime'
import { PaginaNome } from './PaginaNome'

// Instância concreta da página genérica para Cargos. Os hooks são
// criados uma vez aqui (chave de cache 'cargos', base '/cargos').
const hooksCargos = criarHooksNome('cargos', '/cargos')

export function PaginaCargos() {
  return <PaginaNome titulo="Cargos" rotulo="cargo" hooks={hooksCargos} />
}

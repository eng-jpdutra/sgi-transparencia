import { useContext } from 'react'
import { Sessao } from '../contextos/ProvedorSessao'
import type { ContextoSessao } from '../tipos/sessao'

/**
 * Hook de acesso à sessão. Qualquer componente chama useSessao() para
 * saber quem está logado ou disparar entrar/sair.
 *
 * O erro abaixo é uma rede de proteção do desenvolvedor: se alguém
 * usar o hook fora do ProvedorSessao (esquecimento de montagem), a
 * falha aparece na hora, com mensagem clara — em vez de um "undefined"
 * misterioso estourando lá adiante. Fail Fast aplicado ao frontend.
 *
 * (Fica em arquivo próprio, separado do provedor, por uma regra de
 * ferramental do React: manter componentes e hooks em arquivos
 * distintos preserva o "fast refresh" — o hot reload — funcionando.)
 */
export function useSessao(): ContextoSessao {
  const contexto = useContext(Sessao)
  if (contexto === undefined) {
    throw new Error('useSessao deve ser usado dentro de <ProvedorSessao>.')
  }
  return contexto
}

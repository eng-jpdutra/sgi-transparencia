import { useEffect, useState } from 'react'

/**
 * useDebounce — atrasa a propagação de um valor que muda rápido.
 *
 * Caso de uso: o campo de busca textual. Sem debounce, cada tecla
 * dispararia uma chamada à API. Com ele, só depois de ~400ms SEM
 * digitar é que o valor "estabilizado" é entregue — uma única
 * requisição em vez de uma por caractere. (Decisão registrada lá
 * na definição do template canônico, v2.1.)
 */
export function useDebounce<T>(valor: T, atrasoMs = 400): T {
  const [valorAtrasado, setValorAtrasado] = useState(valor)

  useEffect(() => {
    const temporizador = setTimeout(() => setValorAtrasado(valor), atrasoMs)
    // Se 'valor' mudar antes do prazo, cancela o anterior e reinicia.
    return () => clearTimeout(temporizador)
  }, [valor, atrasoMs])

  return valorAtrasado
}

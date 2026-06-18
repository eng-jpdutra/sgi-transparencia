// Utilitário de CPF no frontend — espelha Servicos/Cpf.cs do backend.
// O CPF é a chave civil da pessoa; validar aqui dá feedback imediato
// (Fail Fast no cliente) antes do POST. O backend valida de novo: a LEI
// é do servidor; isto é só cortesia de UX.

/** Mantém só os dígitos (remove pontos, traço, espaços). */
export function normalizarCpf(valor: string): string {
  return valor.replace(/\D/g, '')
}

/** Formata para exibição: '00000000000' -> '000.000.000-00'. */
export function formatarCpf(valor: string): string {
  const d = normalizarCpf(valor)
  if (d.length !== 11) return valor
  return `${d.slice(0, 3)}.${d.slice(3, 6)}.${d.slice(6, 9)}-${d.slice(9)}`
}

/** Valida pelos dois dígitos verificadores (algoritmo da Receita). */
export function cpfValido(valor: string): boolean {
  const d = normalizarCpf(valor)
  if (d.length !== 11) return false
  if (d.split('').every((c) => c === d[0])) return false

  const digito = (parcial: string, pesoInicial: number): number => {
    let soma = 0
    for (let i = 0; i < parcial.length; i++) {
      soma += Number(parcial[i]) * (pesoInicial - i)
    }
    const resto = soma % 11
    return resto < 2 ? 0 : 11 - resto
  }

  const primeiro = digito(d.slice(0, 9), 10)
  const segundo = digito(d.slice(0, 10), 11)
  return primeiro === Number(d[9]) && segundo === Number(d[10])
}

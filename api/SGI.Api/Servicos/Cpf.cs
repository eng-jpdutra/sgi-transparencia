namespace SGI.Api.Servicos;

/// <summary>
/// Utilitário de CPF. Com a refatoração, o CPF passou a ser a chave
/// natural civil da Pessoa (substituindo a matrícula, que migrou para o
/// exercício). Lixo neste campo corromperia a deduplicação de pessoas
/// — justamente o que viabiliza reaproveitar um cadastro existente na
/// admissão. Por isso validamos formato e dígitos verificadores, e
/// armazenamos SEMPRE normalizado (só dígitos), garantindo comparação
/// idêntica em SQLite e PostgreSQL.
/// </summary>
public static class Cpf
{
    /// <summary>Remove tudo que não é dígito (pontos, traços, espaços).</summary>
    public static string Normalizar(string? cpf) =>
        new((cpf ?? string.Empty).Where(char.IsDigit).ToArray());

    /// <summary>
    /// Valida um CPF pelos dois dígitos verificadores (algoritmo da
    /// Receita Federal). Rejeita tamanho diferente de 11 e as sequências
    /// repetidas (000..., 111...), que passam na conta mas são inválidas.
    /// </summary>
    public static bool EhValido(string? cpf)
    {
        var digitos = Normalizar(cpf);
        if (digitos.Length != 11) return false;
        if (digitos.All(c => c == digitos[0])) return false;

        static int CalcularDigito(string parcial, int pesoInicial)
        {
            var soma = 0;
            for (var i = 0; i < parcial.Length; i++)
                soma += (parcial[i] - '0') * (pesoInicial - i);
            var resto = soma % 11;
            return resto < 2 ? 0 : 11 - resto;
        }

        var primeiro = CalcularDigito(digitos[..9], 10);
        var segundo = CalcularDigito(digitos[..10], 11);
        return primeiro == digitos[9] - '0' && segundo == digitos[10] - '0';
    }
}

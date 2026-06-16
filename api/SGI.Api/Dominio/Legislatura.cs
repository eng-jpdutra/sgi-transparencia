namespace SGI.Api.Dominio;

/// <summary>
/// Uma legislatura: o período de mandato (sempre 4 anos) de uma
/// composição do legislativo. É a base temporal de quase tudo no
/// domínio — mandatos, filiações e ocupações se ancoram nela.
///
/// MODELAGEM (decisão de arquitetura): guardamos apenas os DOIS
/// dados-fonte — Numero e AnoInicio. Tudo o mais (datas, nome de
/// exibição) é DERIVADO, calculado a partir deles. Princípio da casa:
/// dado derivável não se armazena, se calcula — assim nunca há um
/// nome no banco "discordando" do ano.
///
/// Herda EntidadeBase: Id, Ativo (soft delete) e auditoria.
/// </summary>
public class Legislatura : EntidadeBase
{
    /// <summary>
    /// Número ordinal da legislatura (ex.: 22 para a "22ª"). É
    /// auto-incremental no nível de negócio (próximo = máximo + 1,
    /// calculado na criação) e único no sistema (constraint no
    /// ContextoDados como última linha de defesa).
    /// </summary>
    public int Numero { get; set; }

    /// <summary>
    /// Ano de início (ex.: 2025). Único dado que o usuário informa.
    /// Dele derivam as datas e o nome.
    /// </summary>
    public int AnoInicio { get; set; }

    // ----- DERIVADOS (calculados, não persistidos) -----
    // [NotMapped] diz ao EF Core: "isto não é coluna, é só lógica".

    /// <summary>Ano de término: início + 3 (duração fixa de 4 anos).</summary>
    [System.ComponentModel.DataAnnotations.Schema.NotMapped]
    public int AnoFim => AnoInicio + 3;

    /// <summary>Primeiro dia da legislatura: 01/01 do ano de início.</summary>
    [System.ComponentModel.DataAnnotations.Schema.NotMapped]
    public DateOnly DataInicio => new(AnoInicio, 1, 1);

    /// <summary>Último dia da legislatura: 31/12 do ano de fim.</summary>
    [System.ComponentModel.DataAnnotations.Schema.NotMapped]
    public DateOnly DataFim => new(AnoFim, 12, 31);

    /// <summary>Nome de exibição: "22ª LEGISLATURA (2025 – 2028)".</summary>
    [System.ComponentModel.DataAnnotations.Schema.NotMapped]
    public string Nome => $"{Numero}ª LEGISLATURA ({AnoInicio} – {AnoFim})";
}

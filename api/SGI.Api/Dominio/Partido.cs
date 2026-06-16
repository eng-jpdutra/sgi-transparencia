namespace SGI.Api.Dominio;

/// <summary>
/// Um partido político (legenda). Diferente de Legislatura, aqui os
/// dados são INFORMADOS pelo usuário (sigla, nome e número de legenda)
/// — não há derivação nem sequência automática.
///
/// Herda EntidadeBase: Id, Ativo (soft delete) e auditoria.
/// </summary>
public class Partido : EntidadeBase
{
    /// <summary>
    /// Sigla do partido (ex.: "PT", "MDB", "PL"). Única no sistema
    /// (índice no ContextoDados) e campo filtrável na pesquisa.
    /// </summary>
    public required string Sigla { get; set; }

    /// <summary>
    /// Nome por extenso (ex.: "Partido dos Trabalhadores").
    /// Único no sistema e filtrável.
    /// </summary>
    public required string Nome { get; set; }

    /// <summary>
    /// Número de legenda eleitoral (ex.: 13, 15, 22). Padrão brasileiro:
    /// dois dígitos (10 a 99). Único no sistema.
    /// </summary>
    public int Numero { get; set; }
}

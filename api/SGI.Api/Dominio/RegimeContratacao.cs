namespace SGI.Api.Dominio;

/// <summary>
/// Um regime de contratação (ex.: "Efetivo", "Comissionado",
/// "Temporário"). Promovido a tabela de domínio (em vez de texto
/// livre no vínculo) para garantir consistência — a decisão que
/// tomamos lá na revisão do modelo de dados.
///
/// Herda EntidadeBase: Id, Ativo (soft delete) e auditoria.
/// </summary>
public class RegimeContratacao : EntidadeBase
{
    /// <summary>
    /// Nome do regime. Único no sistema (índice no ContextoDados) e
    /// campo filtrável na pesquisa.
    /// </summary>
    public required string Nome { get; set; }
}

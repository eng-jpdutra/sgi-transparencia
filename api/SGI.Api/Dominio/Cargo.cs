namespace SGI.Api.Dominio;

/// <summary>
/// Um cargo ocupável por servidores/comissionados (ex.: "Assessor
/// Parlamentar", "Analista Legislativo"). Entidade de domínio simples
/// e independente; será referenciada por Vínculos mais adiante.
///
/// Herda EntidadeBase: Id, Ativo (soft delete) e auditoria.
/// </summary>
public class Cargo : EntidadeBase
{
    /// <summary>
    /// Nome do cargo. Único no sistema (índice no ContextoDados) e
    /// campo filtrável na pesquisa.
    /// </summary>
    public required string Nome { get; set; }
}

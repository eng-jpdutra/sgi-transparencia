namespace SGI.Api.Dominio;

/// <summary>
/// Vínculo — o EXERCÍCIO de uma pessoa como servidora, num período,
/// em um cargo e sob um regime de contratação. É a dimensão temporal
/// do papel servidor: a ficha Servidor diz "pode ser"; o Vínculo diz
/// "exerceu de tal data a tal data, neste cargo".
///
/// Uma pessoa pode ter VÁRIOS vínculos ao longo da vida (ex.: efetivo,
/// depois afasta para mandato, depois retorna em novo vínculo), mas
/// nunca dois vínculos/mandatos vigentes no mesmo período — regra
/// garantida na validação de sobreposição.
///
/// Herda EntidadeBase: Id, Ativo (soft delete) e auditoria.
/// </summary>
public class Vinculo : EntidadeBase
{
    /// <summary>FK para a ficha de servidor.</summary>
    public int ServidorId { get; set; }
    public Servidor? Servidor { get; set; }

    /// <summary>FK para o cargo exercido neste vínculo.</summary>
    public int CargoId { get; set; }
    public Cargo? Cargo { get; set; }

    /// <summary>FK para o regime de contratação deste vínculo.</summary>
    public int RegimeId { get; set; }
    public RegimeContratacao? Regime { get; set; }

    /// <summary>Início do exercício.</summary>
    public DateOnly DataInicio { get; set; }

    /// <summary>
    /// Fim do exercício. NULO = vínculo em aberto (vigente, sem
    /// término definido). É o que distingue um vínculo atual de um
    /// histórico.
    /// </summary>
    public DateOnly? DataFim { get; set; }
}

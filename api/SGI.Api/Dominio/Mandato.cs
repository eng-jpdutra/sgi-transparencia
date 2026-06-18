namespace SGI.Api.Dominio;

/// <summary>
/// Mandato — o EXERCÍCIO de uma pessoa como vereadora, num período,
/// ancorado em uma legislatura. É a dimensão temporal do papel
/// vereador, espelhando o que o Vínculo é para o servidor.
///
/// Datas PRÓPRIAS (não herdadas da legislatura): permitem suplência
/// (assumir no meio) e saída antecipada. DataFim nula = mandato em
/// curso. A regra de não-sobreposição cruza mandatos E vínculos da
/// mesma pessoa — uma pessoa nunca exerce dois papéis ao mesmo tempo.
///
/// Herda EntidadeBase: Id, Ativo (soft delete) e auditoria.
/// </summary>
public class Mandato : EntidadeBase
{
    /// <summary>FK para a ficha de vereador.</summary>
    public int VereadorId { get; set; }
    public Vereador? Vereador { get; set; }

    /// <summary>FK para a legislatura em que o mandato se exerce.</summary>
    public int LegislaturaId { get; set; }
    public Legislatura? Legislatura { get; set; }

    /// <summary>
    /// FK para a matrícula (registro funcional) deste mandato. 1:1 e
    /// obrigatória. Cada mandato gera uma matrícula nova (vereador
    /// reeleito recebe outra), e o número é único em todo o sistema.
    /// </summary>
    public int MatriculaId { get; set; }
    public Matricula? Matricula { get; set; }

    /// <summary>Início do exercício do mandato.</summary>
    public DateOnly DataInicio { get; set; }

    /// <summary>Fim do exercício. NULO = mandato em curso.</summary>
    public DateOnly? DataFim { get; set; }
}

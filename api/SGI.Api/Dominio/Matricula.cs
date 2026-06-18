namespace SGI.Api.Dominio;

/// <summary>
/// Matrícula — o REGISTRO FUNCIONAL. Descoberta de modelagem (RH): a
/// matrícula NÃO pertence à pessoa civil, e sim ao exercício. Uma mesma
/// pessoa acumula várias matrículas ao longo da vida (troca de cargo por
/// novo concurso, ou eleição como vereador), e o número é único em TODA
/// a instituição — servidores e vereadores compartilham o mesmo espaço
/// de numeração.
///
/// Por isso a matrícula vive numa tabela PRÓPRIA: o índice UNIQUE de
/// Numero, concentrado num único lugar, garante a unicidade global no
/// banco. Um índice por tabela (em Vínculo e em Mandato separadamente)
/// não conseguiria — a regra é cruzada entre as duas tabelas, e um
/// índice relacional só vale dentro de uma. Cada Matrícula pertence a
/// EXATAMENTE um exercício: um Vínculo OU um Mandato (1:1).
///
/// Herda EntidadeBase: Id, Ativo (soft delete) e auditoria. O soft
/// delete NÃO libera o número — matrículas não são reaproveitadas, então
/// a unicidade vale para ativos e inativos (checagem com IgnoreQueryFilters).
/// </summary>
public class Matricula : EntidadeBase
{
    /// <summary>Número da matrícula. Único em todo o sistema.</summary>
    public required string Numero { get; set; }

    // ----- Dono do registro funcional (1:1; exatamente um é preenchido) -----

    /// <summary>Vínculo de servidor que esta matrícula identifica (se for o caso).</summary>
    public Vinculo? Vinculo { get; set; }

    /// <summary>Mandato de vereador que esta matrícula identifica (se for o caso).</summary>
    public Mandato? Mandato { get; set; }
}

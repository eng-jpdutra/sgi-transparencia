namespace SGI.Api.Dominio;

/// <summary>
/// Pessoa — o cadastro civil ÚNICO do sistema. Servidor e Vereador
/// são PAPÉIS que apontam para ela (1:1 opcional). Uma pessoa pode
/// possuir a ficha de servidor e/ou a de vereador; qual papel está
/// vigente em dado momento é controlado pela camada de vínculos
/// /mandatos (dimensão temporal), não por estas fichas.
///
/// Herda EntidadeBase: Id, Ativo (soft delete) e auditoria.
/// </summary>
public class Pessoa : EntidadeBase
{
    /// <summary>Nome civil completo.</summary>
    public required string NomeCompleto { get; set; }

    /// <summary>
    /// Matrícula funcional. Obrigatória e única no sistema
    /// (índice no ContextoDados); campo filtrável na pesquisa.
    /// </summary>
    public required string Matricula { get; set; }

    // ----- Papéis (navegações 1:1 opcionais) -----
    // Preenchidas quando a pessoa possui a ficha correspondente.
    public Servidor? Servidor { get; set; }
    public Vereador? Vereador { get; set; }
}

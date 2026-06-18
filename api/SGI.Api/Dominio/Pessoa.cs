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
    /// CPF — identidade civil ÚNICA da pessoa (índice UNIQUE no
    /// ContextoDados). Substitui a antiga matrícula como chave natural:
    /// a matrícula migrou para o registro funcional (ver Matricula),
    /// pois uma pessoa pode ter várias ao longo do tempo. Armazenado
    /// normalizado (só dígitos) — é também a chave de deduplicação que
    /// permite reaproveitar uma pessoa existente na admissão.
    /// </summary>
    public required string Cpf { get; set; }

    // ----- Papéis (navegações 1:1 opcionais) -----
    // Preenchidas quando a pessoa possui a ficha correspondente.
    public Servidor? Servidor { get; set; }
    public Vereador? Vereador { get; set; }
}

namespace SGI.Api.Dominio;

/// <summary>
/// Vereador — ficha de PAPEL de uma pessoa. Relação 1:1 com Pessoa
/// (PessoaId único). A existência desta ficha indica que a pessoa
/// está habilitada como vereadora; mandatos (camada temporal) dirão
/// em quais legislaturas ela de fato exerceu.
///
/// Herda EntidadeBase: Id, Ativo (soft delete) e auditoria.
/// </summary>
public class Vereador : EntidadeBase
{
    /// <summary>FK para a pessoa (1:1, única).</summary>
    public int PessoaId { get; set; }

    /// <summary>Navegação para a pessoa dona desta ficha.</summary>
    public Pessoa? Pessoa { get; set; }

    /// <summary>
    /// Nome pelo qual o vereador é conhecido na atividade legislativa
    /// (ex.: "Dr. João", "Professora Maria"). Obrigatório.
    /// </summary>
    public required string NomeLegislativo { get; set; }
}

namespace SGI.Api.Dominio;

/// <summary>
/// Servidor — ficha de PAPEL de uma pessoa. Relação 1:1 com Pessoa
/// (PessoaId único). A existência desta ficha indica que a pessoa
/// está habilitada como servidora; a vigência efetiva é tratada
/// pelos vínculos (camada temporal, mais adiante).
///
/// Herda EntidadeBase: Id, Ativo (soft delete) e auditoria.
/// </summary>
public class Servidor : EntidadeBase
{
    /// <summary>FK para a pessoa (1:1, única).</summary>
    public int PessoaId { get; set; }

    /// <summary>Navegação para a pessoa dona desta ficha.</summary>
    public Pessoa? Pessoa { get; set; }
}

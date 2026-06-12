namespace SGI.Api.Dominio;

/// <summary>
/// Classe base de TODAS as entidades do sistema.
///
/// Ela materializa duas diretrizes inegociáveis do projeto:
///   1. SOFT DELETE: nada é apagado fisicamente do banco. "Excluir"
///      significa Ativo = false. O ContextoDados esconde automaticamente
///      os inativos de todas as consultas (query filter global).
///   2. AUDITORIA: toda linha sabe quando nasceu e quando foi alterada
///      pela última vez. (O "quem" — usuário responsável — entrará
///      quando a autenticação estiver operante.)
///
/// Por ser "abstract", esta classe nunca vira tabela sozinha;
/// ela só existe para ser herdada.
/// </summary>
public abstract class EntidadeBase
{
    /// <summary>
    /// Chave primária. Inteiro com auto-incremento gerado pelo banco
    /// (comportamento padrão do EF Core para propriedades chamadas "Id").
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Flag de soft delete — e SOMENTE isso.
    /// true  = registro normal, visível em todo o sistema.
    /// false = registro "excluído" logicamente (erro de cadastro, etc).
    ///
    /// IMPORTANTE (decisão de arquitetura): estados de NEGÓCIO
    /// (filiação encerrada, vínculo desligado, lotação finalizada)
    /// NÃO usam este campo — são derivados das colunas de data
    /// (data_desfiliacao, data_desligamento, data_fim...).
    /// </summary>
    public bool Ativo { get; set; } = true;

    /// <summary>
    /// Momento da criação do registro, SEMPRE em UTC.
    /// Preenchido automaticamente pelo ContextoDados ao salvar —
    /// nenhum código de negócio precisa (nem deve) setar isso.
    /// </summary>
    public DateTime CriadoEm { get; set; }

    /// <summary>
    /// Momento da última alteração, SEMPRE em UTC.
    /// Nulo enquanto o registro nunca foi alterado.
    /// Também preenchido automaticamente pelo ContextoDados.
    /// </summary>
    public DateTime? AtualizadoEm { get; set; }
}

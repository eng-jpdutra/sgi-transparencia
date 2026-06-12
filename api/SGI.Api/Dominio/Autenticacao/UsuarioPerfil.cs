namespace SGI.Api.Dominio.Autenticacao;

/// <summary>
/// Tabela associativa da relação N:N entre Usuario e Perfil:
/// "qual usuário possui qual papel".
///
/// Ela herda EntidadeBase de propósito: revogar um perfil de um
/// usuário é um soft delete da atribuição — o que preserva o
/// HISTÓRICO de quem já teve qual acesso e quando. Num sistema
/// auditável de administração pública, saber que fulano teve o
/// papel Admin entre março e julho é informação valiosa.
///
/// A unicidade (um usuário não pode ter o MESMO perfil duas vezes
/// ativo) é garantida por índice no ContextoDados.
/// </summary>
public class UsuarioPerfil : EntidadeBase
{
    /// <summary>FK para o usuário.</summary>
    public int UsuarioId { get; set; }

    /// <summary>
    /// Propriedade de navegação para o usuário.
    /// O "= null!" diz ao compilador: "confie, o EF Core preenche isto
    /// ao carregar do banco". É o padrão recomendado para navegações
    /// obrigatórias quando o Nullable está habilitado.
    /// </summary>
    public Usuario Usuario { get; set; } = null!;

    /// <summary>FK para o perfil.</summary>
    public int PerfilId { get; set; }

    /// <summary>Propriedade de navegação para o perfil.</summary>
    public Perfil Perfil { get; set; } = null!;
}

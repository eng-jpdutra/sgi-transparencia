namespace SGI.Api.Dominio.Autenticacao;

/// <summary>
/// Papel de acesso do RBAC (Role-Based Access Control).
/// Exemplos: "Admin", "Gestor", "Operador".
///
/// Decisão de arquitetura: perfil é TABELA, não enum no código.
/// Criar um perfil novo ("Recepção", "Auditoria") deve ser uma
/// operação de DADOS — feita por um administrador na tela — e não
/// um deploy de nova versão do sistema.
///
/// O nome do perfil viaja dentro do JWT como claim de "role", e é
/// contra ele que os endpoints autorizam (ex.: exigir papel "Admin").
/// </summary>
public class Perfil : EntidadeBase
{
    /// <summary>
    /// Nome do perfil — único no sistema (índice unique no ContextoDados).
    /// É exatamente o texto que aparecerá na claim de role do token.
    /// </summary>
    public required string Nome { get; set; }

    /// <summary>
    /// Descrição livre, para a tela de administração de perfis
    /// explicar a que se destina cada papel.
    /// </summary>
    public string? Descricao { get; set; }

    /// <summary>
    /// Usuários que possuem este perfil (lado N:N).
    /// </summary>
    public ICollection<UsuarioPerfil> Usuarios { get; set; } = new List<UsuarioPerfil>();
}

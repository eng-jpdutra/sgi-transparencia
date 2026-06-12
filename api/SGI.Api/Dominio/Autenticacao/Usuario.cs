namespace SGI.Api.Dominio.Autenticacao;

/// <summary>
/// Conta de acesso ao sistema.
///
/// Conceito importante: USUÁRIO é credencial, não papel funcional.
/// É diferente de Servidor e de Vereador (que são papéis de Pessoa).
/// Quando o domínio de Pessoas for criado, esta entidade ganhará uma
/// FK opcional PessoaId — opcional porque contas administrativas ou
/// de serviço podem não corresponder a uma pessoa física.
/// </summary>
public class Usuario : EntidadeBase
{
    /// <summary>
    /// Identificador de login (único no sistema — índice unique
    /// configurado no ContextoDados).
    /// "required" = o compilador OBRIGA a informar este valor ao criar
    /// um Usuario; impossível nascer usuário sem login. Tipagem forte
    /// fazendo o papel de validação que, em linguagens fracas,
    /// dependeria de disciplina humana.
    /// </summary>
    public required string Login { get; set; }

    /// <summary>
    /// HASH da senha — NUNCA a senha.
    /// O nome da propriedade é deliberado: serve de documentação e de
    /// barreira psicológica. Se alguém um dia tentar gravar uma senha
    /// pura aqui, o nome da coluna denuncia o erro.
    /// O hash será gerado com BCrypt (entra na Etapa 3): algoritmo
    /// adaptativo, com "sal" embutido, projetado para ser LENTO de
    /// propósito — inviabiliza ataques de força bruta ao banco vazado.
    /// </summary>
    public required string SenhaHash { get; set; }

    /// <summary>
    /// Contador de tentativas de login que falharam consecutivamente.
    /// Zera a cada login bem-sucedido. Faz parte do mecanismo de
    /// lockout (proteção contra adivinhação de senha) da Etapa 4.
    /// </summary>
    public int FalhasAcesso { get; set; }

    /// <summary>
    /// Se preenchido e no futuro, a conta está BLOQUEADA até este
    /// momento (UTC). Exemplo de política: 5 falhas seguidas =
    /// bloqueio de 15 minutos. Nulo = conta desbloqueada.
    /// Repare no padrão da casa: o estado "bloqueado" é DERIVADO
    /// de uma data, não de um flag que pode dessincronizar.
    /// </summary>
    public DateTime? BloqueadoAte { get; set; }

    /// <summary>
    /// true = a senha atual é provisória (definida por um administrador
    /// ou pelo seed inicial) e o usuário DEVE trocá-la no próximo login.
    /// O frontend recebe este flag na resposta do login e força o fluxo
    /// de troca antes de liberar o restante do sistema.
    /// </summary>
    public bool DeveTrocarSenha { get; set; }

    /// <summary>
    /// Perfis (papéis de acesso) atribuídos a esta conta.
    /// Relação N:N com Perfil, através da entidade UsuarioPerfil.
    /// </summary>
    public ICollection<UsuarioPerfil> Perfis { get; set; } = new List<UsuarioPerfil>();

    /// <summary>
    /// Refresh tokens emitidos para esta conta (Etapa 4).
    /// </summary>
    public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
}

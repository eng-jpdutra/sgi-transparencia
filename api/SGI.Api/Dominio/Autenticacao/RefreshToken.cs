namespace SGI.Api.Dominio.Autenticacao;

/// <summary>
/// Refresh token: a peça que torna o JWT revogável.
///
/// O problema que esta tabela resolve: um JWT puro vale até expirar,
/// não importa o que aconteça — desativar o usuário não derruba um
/// token já emitido. A solução padrão de mercado:
///   - Access token (JWT) CURTO: ~15 minutos, viaja em cada requisição.
///   - Refresh token LONGO: dias, guardado AQUI, serve apenas para
///     obter um novo access token quando o atual expira.
/// Resultado: revogar os refresh tokens de um usuário (ex.: um
/// comissionado exonerado) derruba o acesso dele em no máximo
/// a vida útil do access token (~15 min).
///
/// Implementação completa do fluxo: Etapa 4.
/// </summary>
public class RefreshToken : EntidadeBase
{
    /// <summary>FK para o dono do token.</summary>
    public int UsuarioId { get; set; }

    /// <summary>Navegação para o usuário (preenchida pelo EF).</summary>
    public Usuario Usuario { get; set; } = null!;

    /// <summary>
    /// HASH do token — nunca o token em si (mesmo princípio da senha).
    /// Se o banco vazar, os hashes não servem para se autenticar.
    /// O token verdadeiro só existe no cliente que o recebeu.
    /// </summary>
    public required string TokenHash { get; set; }

    /// <summary>Validade do token (UTC). Depois disso, é lixo.</summary>
    public DateTime ExpiraEm { get; set; }

    /// <summary>
    /// Momento da revogação (UTC). Nulo = nunca revogado.
    /// Preenchido quando: o token é usado (rotação — cada uso o
    /// substitui por um novo), o usuário faz logout, ou um
    /// administrador derruba os acessos da conta.
    /// Mais uma vez o padrão da casa: estado derivado de DATA.
    /// </summary>
    public DateTime? RevogadoEm { get; set; }
}

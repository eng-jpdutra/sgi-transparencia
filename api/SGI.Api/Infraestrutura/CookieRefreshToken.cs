namespace SGI.Api.Infraestrutura;

/// <summary>
/// Centraliza o trato do cookie que carrega o refresh token (DRY:
/// um único lugar define nome, validade e flags de segurança).
///
/// Por que cookie HttpOnly e não devolver o token no corpo JSON?
///   - HttpOnly: o JavaScript da página NÃO consegue ler o cookie.
///     Mesmo que um ataque de XSS injete script malicioso, ele não
///     alcança o refresh token — a credencial de longa duração fica
///     fora do alcance do código do navegador.
///   - O navegador anexa o cookie automaticamente nas chamadas a
///     /renovar e /sair; o frontend nem precisa (nem consegue) tocá-lo.
/// </summary>
public static class CookieRefreshToken
{
    /// <summary>Nome do cookie. Centralizado para não divergir.</summary>
    public const string Nome = "sgi_refresh";

    /// <summary>
    /// Grava o cookie na resposta, com as flags de segurança corretas.
    /// </summary>
    public static void Gravar(
        HttpContext contexto, string token, DateTime expiraEmUtc)
    {
        contexto.Response.Cookies.Append(Nome, token, new CookieOptions
        {
            // JS não lê (anti-XSS).
            HttpOnly = true,

            // Só trafega em HTTPS. Em DEV (http://localhost) o navegador
            // abre exceção para localhost, então funciona; em PROD exige
            // HTTPS de verdade — exatamente o que queremos.
            Secure = true,

            // SameSite=None: necessário porque o frontend (5173) e a API
            // (5180) são origens distintas. Exige Secure=true (acima).
            SameSite = SameSiteMode.None,

            // O cookie só é enviado para o caminho de autenticação —
            // não vaza para todas as rotas da API.
            Path = "/autenticacao",

            // Expira junto com o refresh token no banco.
            Expires = expiraEmUtc,
        });
    }

    /// <summary>Lê o token do cookie (null se ausente).</summary>
    public static string? Ler(HttpContext contexto)
        => contexto.Request.Cookies.TryGetValue(Nome, out var valor)
            ? valor
            : null;

    /// <summary>Remove o cookie (usado no logout).</summary>
    public static void Remover(HttpContext contexto)
    {
        contexto.Response.Cookies.Delete(Nome, new CookieOptions
        {
            Path = "/autenticacao",
            Secure = true,
            SameSite = SameSiteMode.None,
        });
    }
}

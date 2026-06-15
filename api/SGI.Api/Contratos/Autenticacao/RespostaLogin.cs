namespace SGI.Api.Contratos.Autenticacao;

/// <summary>
/// Resposta do login/renovação bem-sucedidos.
///
/// Etapa 4.1: o refresh token NÃO aparece mais aqui — ele viaja em
/// um cookie HttpOnly (ver CookieRefreshToken), inacessível ao
/// JavaScript. O corpo carrega apenas o access token, que o frontend
/// guarda EM MEMÓRIA (some ao fechar a aba). Cada credencial no seu
/// lugar, com o tempo de vida e a exposição adequados ao seu papel.
/// </summary>
/// <param name="TokenAcesso">
///   O JWT curto (~15 min). Vai no cabeçalho Authorization de cada
///   requisição. Guardado em memória pelo frontend.
/// </param>
/// <param name="ExpiraEmUtc">
///   Quando o access token vence (UTC). Permite renovação proativa.
/// </param>
/// <param name="DeveTrocarSenha">
///   true = senha provisória; o frontend força a troca antes de
///   liberar o sistema.
/// </param>
public record RespostaLogin(
    string TokenAcesso,
    DateTime ExpiraEmUtc,
    bool DeveTrocarSenha);

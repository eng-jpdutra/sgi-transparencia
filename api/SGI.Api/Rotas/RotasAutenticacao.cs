using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using SGI.Api.Contratos.Autenticacao;
using SGI.Api.Dominio.Autenticacao;
using SGI.Api.Infraestrutura;
using SGI.Api.Persistencia;
using SGI.Api.Servicos;

namespace SGI.Api.Rotas;

/// <summary>
/// Rotas do domínio de autenticação.
/// Etapa 4.1: o refresh token agora trafega em cookie HttpOnly
/// (ver CookieRefreshToken), não mais no corpo das requisições.
/// </summary>
public static class RotasAutenticacao
{
    public static void MapearRotasAutenticacao(this WebApplication app)
    {
        var grupo = app.MapGroup("/autenticacao");

        // ==============================================================
        // POST /autenticacao/login
        // ==============================================================
        grupo.MapPost("/login", async (
            RequisicaoLogin requisicao,
            HttpContext contexto,
            ContextoDados db,
            ServicoToken servicoToken,
            IConfiguration configuracao) =>
        {
            if (string.IsNullOrWhiteSpace(requisicao.Login) ||
                string.IsNullOrWhiteSpace(requisicao.Senha))
            {
                return Results.BadRequest(new
                {
                    mensagem = "Login e senha são obrigatórios."
                });
            }

            var usuario = await db.Usuarios
                .Include(u => u.Perfis)
                    .ThenInclude(up => up.Perfil)
                .FirstOrDefaultAsync(u => u.Login == requisicao.Login);

            var agoraUtc = DateTime.UtcNow;

            // Conta bloqueada por lockout?
            if (usuario is not null && usuario.BloqueadoAte > agoraUtc)
            {
                var minutosRestantes = (int)Math.Ceiling(
                    (usuario.BloqueadoAte.Value - agoraUtc).TotalMinutes);

                return Results.Json(
                    new
                    {
                        mensagem = $"Conta temporariamente bloqueada por " +
                                   $"excesso de tentativas. Tente novamente " +
                                   $"em {minutosRestantes} minuto(s)."
                    },
                    statusCode: StatusCodes.Status401Unauthorized);
            }

            // Senha confere?
            if (usuario is null ||
                !BCrypt.Net.BCrypt.Verify(requisicao.Senha, usuario.SenhaHash))
            {
                if (usuario is not null)
                {
                    usuario.FalhasAcesso++;

                    var maxFalhas = configuracao
                        .GetValue("Seguranca:MaxFalhasLogin", 5);
                    var minutosBloqueio = configuracao
                        .GetValue("Seguranca:BloqueioMinutos", 15);

                    if (usuario.FalhasAcesso >= maxFalhas)
                    {
                        usuario.BloqueadoAte =
                            agoraUtc.AddMinutes(minutosBloqueio);
                        usuario.FalhasAcesso = 0;
                    }

                    await db.SaveChangesAsync();
                }

                return Results.Json(
                    new { mensagem = "Credenciais inválidas." },
                    statusCode: StatusCodes.Status401Unauthorized);
            }

            // Sucesso: zera lockout e emite o par de tokens.
            usuario.FalhasAcesso = 0;
            usuario.BloqueadoAte = null;

            var resposta = await EmitirSessaoAsync(
                contexto, db, servicoToken, usuario, agoraUtc);

            return Results.Ok(resposta);
        })
        .AllowAnonymous()
        .RequireRateLimiting("autenticacao");

        // ==============================================================
        // POST /autenticacao/renovar — lê o refresh token do COOKIE.
        // Sem corpo: a credencial é o cookie que o navegador anexa.
        // ==============================================================
        grupo.MapPost("/renovar", async (
            HttpContext contexto,
            ContextoDados db,
            ServicoToken servicoToken) =>
        {
            var tokenRecebido = CookieRefreshToken.Ler(contexto);

            if (string.IsNullOrWhiteSpace(tokenRecebido))
            {
                return Results.Json(
                    new { mensagem = "Sessão inválida ou expirada." },
                    statusCode: StatusCodes.Status401Unauthorized);
            }

            var hash = ServicoToken.CalcularHash(tokenRecebido);

            var tokenArmazenado = await db.RefreshTokens
                .Include(rt => rt.Usuario)
                    .ThenInclude(u => u.Perfis)
                        .ThenInclude(up => up.Perfil)
                .FirstOrDefaultAsync(rt => rt.TokenHash == hash);

            var agoraUtc = DateTime.UtcNow;

            if (tokenArmazenado is null || tokenArmazenado.Usuario is null)
            {
                return Results.Json(
                    new { mensagem = "Sessão inválida ou expirada." },
                    statusCode: StatusCodes.Status401Unauthorized);
            }

            // DETECÇÃO DE ROUBO: token já revogado reaparecendo =
            // cópia em mãos erradas. Derruba TODAS as sessões.
            if (tokenArmazenado.RevogadoEm is not null)
            {
                var sessoes = await db.RefreshTokens
                    .Where(rt => rt.UsuarioId == tokenArmazenado.UsuarioId
                              && rt.RevogadoEm == null)
                    .ToListAsync();

                foreach (var sessao in sessoes)
                    sessao.RevogadoEm = agoraUtc;

                await db.SaveChangesAsync();
                CookieRefreshToken.Remover(contexto);

                return Results.Json(
                    new { mensagem = "Sessão inválida ou expirada." },
                    statusCode: StatusCodes.Status401Unauthorized);
            }

            if (tokenArmazenado.ExpiraEm <= agoraUtc)
            {
                return Results.Json(
                    new { mensagem = "Sessão inválida ou expirada." },
                    statusCode: StatusCodes.Status401Unauthorized);
            }

            // ROTAÇÃO: revoga o atual e emite um par novo.
            tokenArmazenado.RevogadoEm = agoraUtc;

            var resposta = await EmitirSessaoAsync(
                contexto, db, servicoToken, tokenArmazenado.Usuario, agoraUtc);

            return Results.Ok(resposta);
        })
        .AllowAnonymous()
        .RequireRateLimiting("autenticacao");

        // ==============================================================
        // POST /autenticacao/sair — revoga a sessão do cookie e o apaga.
        // ==============================================================
        grupo.MapPost("/sair", async (
            HttpContext contexto,
            ClaimsPrincipal usuarioLogado,
            ContextoDados db) =>
        {
            var tokenRecebido = CookieRefreshToken.Ler(contexto);
            var idUsuario = int.Parse(
                usuarioLogado.FindFirstValue(ClaimTypes.NameIdentifier)!);

            if (!string.IsNullOrWhiteSpace(tokenRecebido))
            {
                var hash = ServicoToken.CalcularHash(tokenRecebido);
                var tokenArmazenado = await db.RefreshTokens
                    .FirstOrDefaultAsync(rt => rt.TokenHash == hash
                                            && rt.UsuarioId == idUsuario);

                if (tokenArmazenado is not null &&
                    tokenArmazenado.RevogadoEm is null)
                {
                    tokenArmazenado.RevogadoEm = DateTime.UtcNow;
                    await db.SaveChangesAsync();
                }
            }

            CookieRefreshToken.Remover(contexto);
            return Results.NoContent();
        })
        .RequireAuthorization();

        // ==============================================================
        // POST /autenticacao/trocar-senha
        // ==============================================================
        grupo.MapPost("/trocar-senha", async (
            RequisicaoTrocaSenha requisicao,
            HttpContext contexto,
            ClaimsPrincipal usuarioLogado,
            ContextoDados db) =>
        {
            if (string.IsNullOrWhiteSpace(requisicao.SenhaAtual) ||
                string.IsNullOrWhiteSpace(requisicao.NovaSenha))
            {
                return Results.BadRequest(new
                {
                    mensagem = "Senha atual e nova senha são obrigatórias."
                });
            }

            if (requisicao.NovaSenha.Length < 8)
            {
                return Results.BadRequest(new
                {
                    mensagem = "A nova senha deve ter ao menos 8 caracteres."
                });
            }

            if (requisicao.NovaSenha == requisicao.SenhaAtual)
            {
                return Results.BadRequest(new
                {
                    mensagem = "A nova senha deve ser diferente da atual."
                });
            }

            var idUsuario = int.Parse(
                usuarioLogado.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var usuario = await db.Usuarios.FirstAsync(u => u.Id == idUsuario);

            if (!BCrypt.Net.BCrypt.Verify(requisicao.SenhaAtual,
                                          usuario.SenhaHash))
            {
                return Results.Json(
                    new { mensagem = "Senha atual incorreta." },
                    statusCode: StatusCodes.Status401Unauthorized);
            }

            usuario.SenhaHash =
                BCrypt.Net.BCrypt.HashPassword(requisicao.NovaSenha);
            usuario.DeveTrocarSenha = false;

            // Senha trocada = todas as sessões caem (expulsa intruso).
            var sessoesAbertas = await db.RefreshTokens
                .Where(rt => rt.UsuarioId == idUsuario
                          && rt.RevogadoEm == null)
                .ToListAsync();

            foreach (var sessao in sessoesAbertas)
                sessao.RevogadoEm = DateTime.UtcNow;

            await db.SaveChangesAsync();
            CookieRefreshToken.Remover(contexto);

            return Results.NoContent();
        })
        .RequireAuthorization();

        // ==============================================================
        // GET /autenticacao/eu — "quem sou eu?" (a partir do token)
        // ==============================================================
        grupo.MapGet("/eu", (ClaimsPrincipal usuarioLogado) =>
        {
            return Results.Ok(new
            {
                id = usuarioLogado.FindFirstValue(ClaimTypes.NameIdentifier),
                login = usuarioLogado.Identity?.Name,
                perfis = usuarioLogado.FindAll(ClaimTypes.Role)
                                      .Select(c => c.Value)
            });
        })
        .RequireAuthorization();
    }

    /// <summary>
    /// Emite uma sessão nova: gera o par de tokens, persiste o hash do
    /// refresh token, grava-o no cookie HttpOnly e devolve a resposta
    /// (só com o access token). Extraído para um único lugar (DRY):
    /// login e renovação compartilham exatamente esta lógica.
    /// </summary>
    private static async Task<RespostaLogin> EmitirSessaoAsync(
        HttpContext contexto,
        ContextoDados db,
        ServicoToken servicoToken,
        Usuario usuario,
        DateTime agoraUtc)
    {
        var (tokenAcesso, expiraEmUtc) = servicoToken.GerarTokenAcesso(usuario);

        var tokenRenovacao = servicoToken.GerarTokenRenovacao();
        var expiraRenovacao = agoraUtc.AddDays(servicoToken.RenovacaoExpiracaoDias);

        db.RefreshTokens.Add(new RefreshToken
        {
            UsuarioId = usuario.Id,
            TokenHash = ServicoToken.CalcularHash(tokenRenovacao),
            ExpiraEm = expiraRenovacao
        });
        await db.SaveChangesAsync();

        // O refresh token vai para o cookie HttpOnly — nunca no corpo.
        CookieRefreshToken.Gravar(contexto, tokenRenovacao, expiraRenovacao);

        return new RespostaLogin(
            tokenAcesso, expiraEmUtc, usuario.DeveTrocarSenha);
    }
}

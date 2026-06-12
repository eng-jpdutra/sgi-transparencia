using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using SGI.Api.Contratos.Autenticacao;
using SGI.Api.Dominio.Autenticacao;
using SGI.Api.Persistencia;
using SGI.Api.Servicos;

namespace SGI.Api.Rotas;

/// <summary>
/// Rotas do domínio de autenticação.
/// Etapa 4: além do login, agora com lockout de força bruta,
/// renovação de token com rotação, logout e troca de senha.
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
            ContextoDados db,
            ServicoToken servicoToken,
            IConfiguration configuracao) =>
        {
            // ---------- FAIL FAST ---------------------------------------
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

            // ---------- LOCKOUT: conta bloqueada? -----------------------
            // Decisão consciente de UX vs sigilo: a mensagem de bloqueio
            // revela que a conta existe — trade-off aceitável num sistema
            // INTERNO, pois orienta o usuário legítimo a esperar em vez
            // de queimar mais tentativas. (Num sistema público, seria
            // mensagem genérica aqui também.)
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

            // ---------- Verificação da senha ----------------------------
            if (usuario is null ||
                !BCrypt.Net.BCrypt.Verify(requisicao.Senha, usuario.SenhaHash))
            {
                // Senha errada para um usuário EXISTENTE: conta a falha.
                if (usuario is not null)
                {
                    usuario.FalhasAcesso++;

                    // Política vinda da CONFIGURAÇÃO, não números mágicos:
                    var maxFalhas = configuracao
                        .GetValue("Seguranca:MaxFalhasLogin", 5);
                    var minutosBloqueio = configuracao
                        .GetValue("Seguranca:BloqueioMinutos", 15);

                    if (usuario.FalhasAcesso >= maxFalhas)
                    {
                        usuario.BloqueadoAte =
                            agoraUtc.AddMinutes(minutosBloqueio);
                        usuario.FalhasAcesso = 0; // recomeça após o bloqueio
                    }

                    await db.SaveChangesAsync();
                }

                // Mensagem genérica (anti-enumeração de usuários).
                return Results.Json(
                    new { mensagem = "Credenciais inválidas." },
                    statusCode: StatusCodes.Status401Unauthorized);
            }

            // ---------- Sucesso: zera o lockout -------------------------
            usuario.FalhasAcesso = 0;
            usuario.BloqueadoAte = null;

            // ---------- Emite o PAR de tokens ---------------------------
            // 1) Access token (JWT, ~15 min) — viaja em toda requisição.
            var (tokenAcesso, expiraEmUtc) =
                servicoToken.GerarTokenAcesso(usuario);

            // 2) Refresh token (dias) — só o HASH vai para o banco;
            //    o token verdadeiro só existe na resposta ao cliente.
            var tokenRenovacao = servicoToken.GerarTokenRenovacao();
            db.RefreshTokens.Add(new RefreshToken
            {
                UsuarioId = usuario.Id,
                TokenHash = ServicoToken.CalcularHash(tokenRenovacao),
                ExpiraEm = agoraUtc.AddDays(servicoToken.RenovacaoExpiracaoDias)
            });

            await db.SaveChangesAsync();

            return Results.Ok(new RespostaLogin(
                tokenAcesso, expiraEmUtc, tokenRenovacao,
                usuario.DeveTrocarSenha));
        })
        .AllowAnonymous();

        // ==============================================================
        // POST /autenticacao/renovar — troca refresh token válido por
        // um PAR NOVO de tokens (rotação). Anônimo: possuir o token
        // de renovação É a credencial neste fluxo.
        // ==============================================================
        grupo.MapPost("/renovar", async (
            RequisicaoRenovacao requisicao,
            ContextoDados db,
            ServicoToken servicoToken) =>
        {
            // ---------- FAIL FAST ---------------------------------------
            if (string.IsNullOrWhiteSpace(requisicao.TokenRenovacao))
            {
                return Results.BadRequest(new
                {
                    mensagem = "Token de renovação é obrigatório."
                });
            }

            // Busca pelo HASH (nunca guardamos o token em si).
            var hash = ServicoToken.CalcularHash(requisicao.TokenRenovacao);

            var tokenArmazenado = await db.RefreshTokens
                .Include(rt => rt.Usuario)
                    .ThenInclude(u => u.Perfis)
                        .ThenInclude(up => up.Perfil)
                .FirstOrDefaultAsync(rt => rt.TokenHash == hash);

            var agoraUtc = DateTime.UtcNow;

            // Token desconhecido OU usuário inativado (o query filter
            // global derruba o Include do usuário inativo — é assim que
            // a exoneração corta a renovação): 401 genérico.
            if (tokenArmazenado is null || tokenArmazenado.Usuario is null)
            {
                return Results.Json(
                    new { mensagem = "Sessão inválida ou expirada." },
                    statusCode: StatusCodes.Status401Unauthorized);
            }

            // ---------- DETECÇÃO DE ROUBO -------------------------------
            // Um token JÁ REVOGADO sendo reapresentado significa que duas
            // partes possuem a mesma cópia — e uma delas é um ladrão
            // (a rotação garante que o dono legítimo sempre tem o mais
            // novo). Resposta enterprise: derrubar TODAS as sessões do
            // usuário e forçar novo login. Inconveniência mínima para a
            // vítima; fim de jogo para o atacante.
            if (tokenArmazenado.RevogadoEm is not null)
            {
                var sessoesDoUsuario = await db.RefreshTokens
                    .Where(rt => rt.UsuarioId == tokenArmazenado.UsuarioId
                              && rt.RevogadoEm == null)
                    .ToListAsync();

                foreach (var sessao in sessoesDoUsuario)
                    sessao.RevogadoEm = agoraUtc;

                await db.SaveChangesAsync();

                return Results.Json(
                    new { mensagem = "Sessão inválida ou expirada." },
                    statusCode: StatusCodes.Status401Unauthorized);
            }

            // ---------- Expirado naturalmente? --------------------------
            if (tokenArmazenado.ExpiraEm <= agoraUtc)
            {
                return Results.Json(
                    new { mensagem = "Sessão inválida ou expirada." },
                    statusCode: StatusCodes.Status401Unauthorized);
            }

            // ---------- ROTAÇÃO: revoga o antigo, emite par novo --------
            tokenArmazenado.RevogadoEm = agoraUtc;

            var novoTokenRenovacao = servicoToken.GerarTokenRenovacao();
            db.RefreshTokens.Add(new RefreshToken
            {
                UsuarioId = tokenArmazenado.UsuarioId,
                TokenHash = ServicoToken.CalcularHash(novoTokenRenovacao),
                ExpiraEm = agoraUtc.AddDays(servicoToken.RenovacaoExpiracaoDias)
            });

            await db.SaveChangesAsync();

            var (tokenAcesso, expiraEmUtc) =
                servicoToken.GerarTokenAcesso(tokenArmazenado.Usuario);

            return Results.Ok(new RespostaLogin(
                tokenAcesso, expiraEmUtc, novoTokenRenovacao,
                tokenArmazenado.Usuario.DeveTrocarSenha));
        })
        .AllowAnonymous();

        // ==============================================================
        // POST /autenticacao/sair — logout REAL: revoga o refresh token.
        // (Descartar tokens só no frontend é logout de mentira: o
        // refresh continuaria utilizável por quem o tivesse copiado.)
        // ==============================================================
        grupo.MapPost("/sair", async (
            RequisicaoRenovacao requisicao,
            ClaimsPrincipal usuarioLogado,
            ContextoDados db) =>
        {
            if (string.IsNullOrWhiteSpace(requisicao.TokenRenovacao))
            {
                return Results.BadRequest(new
                {
                    mensagem = "Token de renovação é obrigatório."
                });
            }

            var idUsuario = int.Parse(
                usuarioLogado.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var hash = ServicoToken.CalcularHash(requisicao.TokenRenovacao);

            // Defesa em profundidade: o token a revogar precisa existir
            // E pertencer a quem está logado — ninguém desloga os outros.
            var tokenArmazenado = await db.RefreshTokens
                .FirstOrDefaultAsync(rt => rt.TokenHash == hash
                                        && rt.UsuarioId == idUsuario);

            if (tokenArmazenado is not null &&
                tokenArmazenado.RevogadoEm is null)
            {
                tokenArmazenado.RevogadoEm = DateTime.UtcNow;
                await db.SaveChangesAsync();
            }

            // 204: deu certo, nada a dizer. (Idempotente: sair duas
            // vezes não é erro.)
            return Results.NoContent();
        })
        .RequireAuthorization();

        // ==============================================================
        // POST /autenticacao/trocar-senha — fecha o ciclo do
        // DeveTrocarSenha e serve para trocas voluntárias.
        // ==============================================================
        grupo.MapPost("/trocar-senha", async (
            RequisicaoTrocaSenha requisicao,
            ClaimsPrincipal usuarioLogado,
            ContextoDados db) =>
        {
            // ---------- FAIL FAST: valida ANTES de tocar o banco --------
            if (string.IsNullOrWhiteSpace(requisicao.SenhaAtual) ||
                string.IsNullOrWhiteSpace(requisicao.NovaSenha))
            {
                return Results.BadRequest(new
                {
                    mensagem = "Senha atual e nova senha são obrigatórias."
                });
            }

            // Política mínima de senha. (Evolução futura — comprimento,
            // complexidade, senhas vazadas — entra por configuração.)
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

            // Quem está pedindo? Vem do TOKEN (claim sub), nunca do corpo
            // da requisição — usuário troca a PRÓPRIA senha, ponto.
            var idUsuario = int.Parse(
                usuarioLogado.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var usuario = await db.Usuarios.FirstAsync(u => u.Id == idUsuario);

            // Prova de posse da senha vigente (anti-sequestro de sessão).
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

            // Governança: senha trocada = TODAS as sessões abertas caem.
            // Se a troca foi motivada por suspeita de comprometimento,
            // isto expulsa o intruso junto.
            var sessoesAbertas = await db.RefreshTokens
                .Where(rt => rt.UsuarioId == idUsuario
                          && rt.RevogadoEm == null)
                .ToListAsync();

            foreach (var sessao in sessoesAbertas)
                sessao.RevogadoEm = DateTime.UtcNow;

            await db.SaveChangesAsync();

            return Results.NoContent();
        })
        .RequireAuthorization();

        // ==============================================================
        // GET /autenticacao/eu — inspeção: "quem sou eu?" (inalterado)
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
}

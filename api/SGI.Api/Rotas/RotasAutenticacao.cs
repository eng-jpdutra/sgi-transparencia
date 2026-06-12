using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using SGI.Api.Contratos.Autenticacao;
using SGI.Api.Persistencia;
using SGI.Api.Servicos;

namespace SGI.Api.Rotas;

/// <summary>
/// Rotas do domínio de autenticação — o PRIMEIRO módulo de rotas
/// do projeto, inaugurando o padrão da casa:
///   - Um arquivo por domínio, dentro de Rotas/.
///   - Um extension method que o Program.cs chama em UMA linha.
/// O Program.cs continua sendo a visão geral; o detalhe vive aqui.
/// </summary>
public static class RotasAutenticacao
{
    public static void MapearRotasAutenticacao(this WebApplication app)
    {
        // MapGroup: prefixo comum a todas as rotas deste módulo (DRY).
        var grupo = app.MapGroup("/autenticacao");

        // ==============================================================
        // POST /autenticacao/login — público por natureza
        // (é impossível exigir token de quem ainda não tem token).
        // ==============================================================
        grupo.MapPost("/login", async (
            RequisicaoLogin requisicao,   // o corpo JSON, desserializado
            ContextoDados db,             // injeção de dependência: o .NET
            ServicoToken servicoToken) => // entrega os serviços registrados
        {
            // ---------- FAIL FAST: valida ANTES de tocar o banco ------
            if (string.IsNullOrWhiteSpace(requisicao.Login) ||
                string.IsNullOrWhiteSpace(requisicao.Senha))
            {
                return Results.BadRequest(new
                {
                    mensagem = "Login e senha são obrigatórios."
                });
            }

            // ---------- Busca o usuário com seus perfis ---------------
            // Include/ThenInclude: traz junto os perfis (necessários
            // para as claims de role) em UMA única consulta SQL.
            // O query filter global já descarta usuário/perfil inativo.
            var usuario = await db.Usuarios
                .Include(u => u.Perfis)
                    .ThenInclude(up => up.Perfil)
                .FirstOrDefaultAsync(u => u.Login == requisicao.Login);

            // ---------- Verificação da senha ---------------------------
            // BCrypt.Verify: re-aplica o hash na senha informada (usando
            // o "sal" embutido no hash gravado) e compara os resultados.
            // A senha pura NUNCA é comparada nem armazenada.
            //
            // SEGURANÇA — mensagem deliberadamente GENÉRICA: não dizemos
            // se o login não existe ou se a senha está errada. Resposta
            // específica permitiria a um atacante descobrir quais logins
            // existem no sistema (ataque de enumeração de usuários).
            if (usuario is null ||
                !BCrypt.Net.BCrypt.Verify(requisicao.Senha, usuario.SenhaHash))
            {
                return Results.Json(
                    new { mensagem = "Credenciais inválidas." },
                    statusCode: StatusCodes.Status401Unauthorized);
            }

            // [ETAPA 4] O lockout entrará aqui: contagem de falhas,
            //           verificação de BloqueadoAte e emissão do
            //           refresh token junto com o access token.

            // ---------- Identidade provada: emite o token --------------
            var (token, expiraEmUtc) = servicoToken.GerarTokenAcesso(usuario);

            return Results.Ok(new RespostaLogin(
                token, expiraEmUtc, usuario.DeveTrocarSenha));
        })
        .AllowAnonymous(); // explícito > implícito: este endpoint é público.

        // ==============================================================
        // GET /autenticacao/eu — endpoint de inspeção: "quem sou eu?"
        // Serve para o frontend (e para nós, testando) confirmar que o
        // token funciona e ver as claims que o pipeline extraiu dele.
        // ==============================================================
        grupo.MapGet("/eu", (ClaimsPrincipal usuarioLogado) =>
        {
            // ClaimsPrincipal: a identidade que o middleware de
            // autenticação montou A PARTIR DO TOKEN — repare que esta
            // rota não toca o banco. É o "stateless" do JWT na prática.
            return Results.Ok(new
            {
                id = usuarioLogado.FindFirstValue(ClaimTypes.NameIdentifier),
                login = usuarioLogado.Identity?.Name,
                perfis = usuarioLogado.FindAll(ClaimTypes.Role)
                                      .Select(c => c.Value)
            });
        })
        // RequireAuthorization SEM argumentos = basta estar autenticado.
        // Com argumento (ex.: RequireAuthorization("Admin")) = RBAC,
        // que estreará nas rotas de domínio.
        .RequireAuthorization();
    }
}

using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using SGI.Api.Contratos;
using SGI.Api.Contratos.Usuarios;
using SGI.Api.Dominio.Autenticacao;
using SGI.Api.Persistencia;
using SGI.Api.Servicos;

namespace SGI.Api.Rotas;

/// <summary>
/// Rotas de gestão de Usuários — módulo EXCLUSIVO de Admin (todo o
/// grupo exige o perfil, não apenas a escrita). Gerenciar contas é
/// função administrativa por inteiro.
///
///   GET    /usuarios            -> listagem paginada + filtros
///   GET    /usuarios/{id}       -> um usuário
///   POST   /usuarios            -> criar (gera senha provisória)
///   PUT    /usuarios/{id}       -> editar perfis
///   POST   /usuarios/{id}/resetar-senha -> nova senha provisória
///   DELETE /usuarios/{id}       -> inativar (soft delete)
///
/// SALVAGUARDAS anti-lockout (Fail Fast): o Admin não pode inativar a
/// própria conta nem remover o próprio perfil Admin, e o sistema nunca
/// fica sem ao menos um Admin ativo.
/// </summary>
public static class RotasUsuarios
{
    private const int TamanhoMaximoPagina = 100;
    private const string PerfilAdmin = "Admin";

    public static void MapearRotasUsuarios(this WebApplication app)
    {
        // O grupo INTEIRO exige Admin (diferente do RBAC assimétrico
        // dos módulos de domínio).
        var grupo = app.MapGroup("/usuarios")
            .RequireAuthorization(p => p.RequireRole(PerfilAdmin));

        // ==============================================================
        // GET /usuarios — listagem paginada.
        // ==============================================================
        grupo.MapGet("/", async (
            ContextoDados db,
            int pagina = 1,
            int tamanhoPagina = 20,
            string? busca = null,
            string? ordenarPor = null,
            bool descendente = false,
            bool incluirInativos = false) =>
        {
            if (pagina < 1) pagina = 1;
            if (tamanhoPagina < 1) tamanhoPagina = 20;
            if (tamanhoPagina > TamanhoMaximoPagina)
                tamanhoPagina = TamanhoMaximoPagina;

            var consulta = db.Usuarios
                .AsNoTracking()
                .Include(u => u.Perfis).ThenInclude(up => up.Perfil)
                .AsQueryable();

            if (incluirInativos)
                consulta = consulta.IgnoreQueryFilters();

            if (!string.IsNullOrWhiteSpace(busca))
            {
                var termo = busca.ToLower();
                consulta = consulta.Where(u => u.Login.ToLower().Contains(termo));
            }

            var total = await consulta.CountAsync();

            consulta = (ordenarPor?.ToLower(), descendente) switch
            {
                ("login", true) => consulta.OrderByDescending(u => u.Login),
                ("ativo", false) => consulta.OrderBy(u => u.Ativo),
                ("ativo", true) => consulta.OrderByDescending(u => u.Ativo),
                _ => consulta.OrderBy(u => u.Login),
            };

            var usuarios = await consulta
                .Skip((pagina - 1) * tamanhoPagina)
                .Take(tamanhoPagina)
                .ToListAsync();

            var itens = usuarios.Select(ParaSaida).ToList();

            return Results.Ok(new ResultadoPaginado<UsuarioSaida>(
                itens, total, pagina, tamanhoPagina));
        });

        // ==============================================================
        // GET /usuarios/{id}
        // ==============================================================
        grupo.MapGet("/{id:int}", async (int id, ContextoDados db) =>
        {
            var usuario = await db.Usuarios
                .AsNoTracking()
                .IgnoreQueryFilters()
                .Include(u => u.Perfis).ThenInclude(up => up.Perfil)
                .FirstOrDefaultAsync(u => u.Id == id);

            return usuario is null
                ? Results.NotFound(new { mensagem = "Usuário não encontrado." })
                : Results.Ok(ParaSaida(usuario));
        });

        // ==============================================================
        // POST /usuarios — criar (gera senha provisória).
        // ==============================================================
        grupo.MapPost("/", async (UsuarioCriacao entrada, ContextoDados db) =>
        {
            if (string.IsNullOrWhiteSpace(entrada.Login))
                return Results.BadRequest(new { mensagem = "O login é obrigatório." });

            if (entrada.PerfilIds is null || entrada.PerfilIds.Length == 0)
                return Results.BadRequest(new { mensagem = "Selecione ao menos um perfil." });

            var login = entrada.Login.Trim().ToLower();

            // Duplicidade de login (Fail Fast antes da constraint unique).
            var loginExiste = await db.Usuarios
                .IgnoreQueryFilters()
                .AnyAsync(u => u.Login.ToLower() == login);
            if (loginExiste)
                return Results.BadRequest(new { mensagem = "Já existe um usuário com este login." });

            // Os perfis informados existem?
            var perfis = await db.Perfis
                .Where(p => entrada.PerfilIds.Contains(p.Id))
                .ToListAsync();
            if (perfis.Count != entrada.PerfilIds.Distinct().Count())
                return Results.BadRequest(new { mensagem = "Um ou mais perfis são inválidos." });

            // Gera a senha provisória e guarda apenas o HASH.
            var senhaProvisoria = GeradorSenha.Gerar();

            var usuario = new Usuario
            {
                Login = login,
                SenhaHash = BCrypt.Net.BCrypt.HashPassword(senhaProvisoria),
                DeveTrocarSenha = true, // troca obrigatória no 1º acesso
                Perfis = perfis.Select(p => new UsuarioPerfil { PerfilId = p.Id }).ToList(),
            };

            db.Usuarios.Add(usuario);
            await db.SaveChangesAsync();

            // Recarrega com os perfis para a resposta.
            await db.Entry(usuario).Collection(u => u.Perfis).Query()
                .Include(up => up.Perfil).LoadAsync();

            // A senha provisória sai AQUI, uma única vez.
            return Results.Created($"/usuarios/{usuario.Id}",
                new UsuarioCriadoSaida(ParaSaida(usuario), senhaProvisoria));
        });

        // ==============================================================
        // PUT /usuarios/{id} — editar perfis.
        // ==============================================================
        grupo.MapPut("/{id:int}", async (
            int id, UsuarioEdicao entrada, ClaimsPrincipal solicitante,
            ContextoDados db) =>
        {
            if (entrada.PerfilIds is null || entrada.PerfilIds.Length == 0)
                return Results.BadRequest(new { mensagem = "Selecione ao menos um perfil." });

            var usuario = await db.Usuarios
                .Include(u => u.Perfis).ThenInclude(up => up.Perfil)
                .FirstOrDefaultAsync(u => u.Id == id);

            if (usuario is null)
                return Results.NotFound(new { mensagem = "Usuário não encontrado." });

            var perfis = await db.Perfis
                .Where(p => entrada.PerfilIds.Contains(p.Id))
                .ToListAsync();
            if (perfis.Count != entrada.PerfilIds.Distinct().Count())
                return Results.BadRequest(new { mensagem = "Um ou mais perfis são inválidos." });

            // SALVAGUARDA: o Admin não pode remover o PRÓPRIO perfil
            // Admin (evita se rebaixar e perder acesso a este módulo).
            var idSolicitante = int.Parse(
                solicitante.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var perderaAdmin = usuario.Id == idSolicitante
                && usuario.Perfis.Any(up => up.Perfil.Nome == PerfilAdmin)
                && !perfis.Any(p => p.Nome == PerfilAdmin);
            if (perderaAdmin)
                return Results.BadRequest(new
                {
                    mensagem = "Você não pode remover o próprio perfil de Administrador."
                });

            // SALVAGUARDA: não deixar o sistema sem nenhum Admin ativo.
            if (await RemoveriaUltimoAdmin(db, usuario, perfis))
                return Results.BadRequest(new
                {
                    mensagem = "Operação negada: deixaria o sistema sem nenhum administrador ativo."
                });

            // Substitui o conjunto de perfis (remove os atuais, adiciona os novos).
            db.UsuariosPerfis.RemoveRange(usuario.Perfis);
            usuario.Perfis = perfis
                .Select(p => new UsuarioPerfil { UsuarioId = usuario.Id, PerfilId = p.Id })
                .ToList();

            await db.SaveChangesAsync();
            await db.Entry(usuario).Collection(u => u.Perfis).Query()
                .Include(up => up.Perfil).LoadAsync();

            return Results.Ok(ParaSaida(usuario));
        });

        // ==============================================================
        // POST /usuarios/{id}/resetar-senha — nova senha provisória.
        // ==============================================================
        grupo.MapPost("/{id:int}/resetar-senha", async (int id, ContextoDados db) =>
        {
            var usuario = await db.Usuarios
                .Include(u => u.Perfis).ThenInclude(up => up.Perfil)
                .FirstOrDefaultAsync(u => u.Id == id);

            if (usuario is null)
                return Results.NotFound(new { mensagem = "Usuário não encontrado." });

            var novaSenha = GeradorSenha.Gerar();
            usuario.SenhaHash = BCrypt.Net.BCrypt.HashPassword(novaSenha);
            usuario.DeveTrocarSenha = true;
            usuario.FalhasAcesso = 0;
            usuario.BloqueadoAte = null; // o reset também desbloqueia

            // Revoga as sessões abertas: a senha mudou, os acessos caem.
            var sessoes = await db.RefreshTokens
                .Where(rt => rt.UsuarioId == id && rt.RevogadoEm == null)
                .ToListAsync();
            foreach (var s in sessoes) s.RevogadoEm = DateTime.UtcNow;

            await db.SaveChangesAsync();

            return Results.Ok(new { senhaProvisoria = novaSenha });
        });

        // ==============================================================
        // DELETE /usuarios/{id} — inativar (soft delete).
        // ==============================================================
        grupo.MapDelete("/{id:int}", async (
            int id, ClaimsPrincipal solicitante, ContextoDados db) =>
        {
            var usuario = await db.Usuarios
                .Include(u => u.Perfis).ThenInclude(up => up.Perfil)
                .FirstOrDefaultAsync(u => u.Id == id);

            if (usuario is null)
                return Results.NotFound(new { mensagem = "Usuário não encontrado." });

            // SALVAGUARDA: não inativar a própria conta.
            var idSolicitante = int.Parse(
                solicitante.FindFirstValue(ClaimTypes.NameIdentifier)!);
            if (usuario.Id == idSolicitante)
                return Results.BadRequest(new
                {
                    mensagem = "Você não pode inativar a própria conta."
                });

            // SALVAGUARDA: não inativar o último Admin ativo.
            if (await RemoveriaUltimoAdmin(db, usuario, perfisFuturos: null))
                return Results.BadRequest(new
                {
                    mensagem = "Operação negada: deixaria o sistema sem nenhum administrador ativo."
                });

            db.Usuarios.Remove(usuario); // vira soft delete no contexto

            // Inativar = derrubar as sessões também.
            var sessoes = await db.RefreshTokens
                .Where(rt => rt.UsuarioId == id && rt.RevogadoEm == null)
                .ToListAsync();
            foreach (var s in sessoes) s.RevogadoEm = DateTime.UtcNow;

            await db.SaveChangesAsync();
            return Results.NoContent();
        });
    }

    /// <summary>
    /// Verifica se uma operação deixaria o sistema sem nenhum Admin
    /// ativo. Usada tanto na edição de perfis (passando os perfis
    /// futuros do usuário) quanto na inativação (perfisFuturos = null,
    /// significando que o usuário deixará de ter qualquer perfil ativo).
    /// </summary>
    private static async Task<bool> RemoveriaUltimoAdmin(
        ContextoDados db, Usuario usuario, List<Perfil>? perfisFuturos)
    {
        var eraAdmin = usuario.Perfis.Any(up => up.Perfil.Nome == PerfilAdmin);
        if (!eraAdmin) return false; // não era admin: não afeta a contagem

        // Continuará admin após a operação? (na inativação, não)
        var continuaraAdmin = perfisFuturos?.Any(p => p.Nome == PerfilAdmin) ?? false;
        if (continuaraAdmin) return false;

        // Quantos OUTROS admins ativos existem?
        var outrosAdmins = await db.Usuarios
            .Where(u => u.Id != usuario.Id)
            .Where(u => u.Perfis.Any(up => up.Perfil.Nome == PerfilAdmin))
            .CountAsync();

        return outrosAdmins == 0; // se não há outro, esta operação é proibida
    }

    /// <summary>Mapeia a entidade para o DTO de saída (sem senha/hash).</summary>
    private static UsuarioSaida ParaSaida(Usuario u) => new(
        u.Id,
        u.Login,
        u.DeveTrocarSenha,
        u.BloqueadoAte > DateTime.UtcNow, // bloqueado = lockout vigente
        u.Ativo,
        u.Perfis.Select(up => new PerfilSaida(
            up.Perfil.Id, up.Perfil.Nome, up.Perfil.Descricao)).ToList());
}

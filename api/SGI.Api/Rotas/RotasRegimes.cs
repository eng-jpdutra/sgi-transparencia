using Microsoft.EntityFrameworkCore;
using SGI.Api.Contratos;
using SGI.Api.Contratos.Regimes;
using SGI.Api.Dominio;
using SGI.Api.Persistencia;

namespace SGI.Api.Rotas;

/// <summary>
/// Rotas do domínio Regimes de Contratação — template canônico
/// (idêntico a Cargos). RBAC assimétrico: ler exige autenticação;
/// escrever exige Admin.
/// </summary>
public static class RotasRegimes
{
    private const int TamanhoMaximoPagina = 100;

    public static void MapearRotasRegimes(this WebApplication app)
    {
        var grupo = app.MapGroup("/regimes").RequireAuthorization();

        // GET /regimes — listagem paginada + filtro + ordenação.
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
            if (tamanhoPagina > TamanhoMaximoPagina) tamanhoPagina = TamanhoMaximoPagina;

            var consulta = db.Regimes.AsNoTracking();
            if (incluirInativos) consulta = consulta.IgnoreQueryFilters();

            if (!string.IsNullOrWhiteSpace(busca))
            {
                var termo = busca.ToLower();
                consulta = consulta.Where(r => r.Nome.ToLower().Contains(termo));
            }

            var total = await consulta.CountAsync();

            consulta = (ordenarPor?.ToLower(), descendente) switch
            {
                ("nome", true)  => consulta.OrderByDescending(r => r.Nome),
                ("ativo", false) => consulta.OrderBy(r => r.Ativo),
                ("ativo", true)  => consulta.OrderByDescending(r => r.Ativo),
                _               => consulta.OrderBy(r => r.Nome), // padrão
            };

            var itens = await consulta
                .Skip((pagina - 1) * tamanhoPagina)
                .Take(tamanhoPagina)
                .Select(r => new RegimeSaida(r.Id, r.Nome, r.Ativo))
                .ToListAsync();

            return Results.Ok(new ResultadoPaginado<RegimeSaida>(
                itens, total, pagina, tamanhoPagina));
        });

        // GET /regimes/{id}
        grupo.MapGet("/{id:int}", async (int id, ContextoDados db) =>
        {
            var regime = await db.Regimes.AsNoTracking().FirstOrDefaultAsync(r => r.Id == id);
            return regime is null
                ? Results.NotFound(new { mensagem = "Regime não encontrado." })
                : Results.Ok(new RegimeSaida(regime.Id, regime.Nome, regime.Ativo));
        });

        // POST /regimes — criar (somente Admin).
        grupo.MapPost("/", async (RegimeEntrada entrada, ContextoDados db) =>
        {
            var erro = await ValidarAsync(entrada, db, idAtual: null);
            if (erro is not null) return Results.BadRequest(new { mensagem = erro });

            var regime = new RegimeContratacao { Nome = entrada.Nome!.Trim() };
            db.Regimes.Add(regime);
            await db.SaveChangesAsync();

            return Results.Created($"/regimes/{regime.Id}",
                new RegimeSaida(regime.Id, regime.Nome, regime.Ativo));
        })
        .RequireAuthorization(p => p.RequireRole("Admin"));

        // PUT /regimes/{id} — editar (somente Admin).
        grupo.MapPut("/{id:int}", async (int id, RegimeEntrada entrada, ContextoDados db) =>
        {
            var regime = await db.Regimes.FirstOrDefaultAsync(r => r.Id == id);
            if (regime is null) return Results.NotFound(new { mensagem = "Regime não encontrado." });

            var erro = await ValidarAsync(entrada, db, idAtual: id);
            if (erro is not null) return Results.BadRequest(new { mensagem = erro });

            regime.Nome = entrada.Nome!.Trim();
            await db.SaveChangesAsync();

            return Results.Ok(new RegimeSaida(regime.Id, regime.Nome, regime.Ativo));
        })
        .RequireAuthorization(p => p.RequireRole("Admin"));

        // DELETE /regimes/{id} — inativar (soft delete; só Admin).
        grupo.MapDelete("/{id:int}", async (int id, ContextoDados db) =>
        {
            var regime = await db.Regimes.FirstOrDefaultAsync(r => r.Id == id);
            if (regime is null) return Results.NotFound(new { mensagem = "Regime não encontrado." });

            db.Regimes.Remove(regime); // soft delete no contexto
            await db.SaveChangesAsync();
            return Results.NoContent();
        })
        .RequireAuthorization(p => p.RequireRole("Admin"));
    }

    private static async Task<string?> ValidarAsync(
        RegimeEntrada entrada, ContextoDados db, int? idAtual)
    {
        if (string.IsNullOrWhiteSpace(entrada.Nome))
            return "O nome é obrigatório.";

        var nome = entrada.Nome.Trim().ToLower();
        var duplicado = await db.Regimes
            .IgnoreQueryFilters()
            .AnyAsync(r => r.Nome.ToLower() == nome && r.Id != idAtual);

        return duplicado ? "Já existe um regime com este nome." : null;
    }
}

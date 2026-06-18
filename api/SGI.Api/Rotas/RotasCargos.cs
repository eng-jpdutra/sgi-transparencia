using Microsoft.EntityFrameworkCore;
using SGI.Api.Contratos;
using SGI.Api.Contratos.Cargos;
using SGI.Api.Dominio;
using SGI.Api.Persistencia;

namespace SGI.Api.Rotas;

/// <summary>
/// Rotas do domínio Cargos — template canônico (igual a Partidos),
/// com ordenação server-side. RBAC assimétrico: ler exige
/// autenticação; escrever exige Admin.
/// </summary>
public static class RotasCargos
{
    private const int TamanhoMaximoPagina = 100;

    public static void MapearRotasCargos(this WebApplication app)
    {
        var grupo = app.MapGroup("/cargos").RequireAuthorization();

        // GET /cargos — listagem paginada + filtro + ordenação.
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

            var consulta = db.Cargos.AsNoTracking();
            if (incluirInativos) consulta = consulta.IgnoreQueryFilters();

            if (!string.IsNullOrWhiteSpace(busca))
            {
                var termo = busca.ToLower();
                consulta = consulta.Where(c => c.Nome.ToLower().Contains(termo));
            }

            var total = await consulta.CountAsync();

            consulta = (ordenarPor?.ToLower(), descendente) switch
            {
                ("nome", true)  => consulta.OrderByDescending(c => c.Nome),
                ("ativo", false) => consulta.OrderBy(c => c.Ativo),
                ("ativo", true)  => consulta.OrderByDescending(c => c.Ativo),
                _               => consulta.OrderBy(c => c.Nome), // padrão
            };

            var itens = await consulta
                .Skip((pagina - 1) * tamanhoPagina)
                .Take(tamanhoPagina)
                .Select(c => new CargoSaida(c.Id, c.Nome, c.Ativo))
                .ToListAsync();

            return Results.Ok(new ResultadoPaginado<CargoSaida>(
                itens, total, pagina, tamanhoPagina));
        });

        // GET /cargos/{id}
        grupo.MapGet("/{id:int}", async (int id, ContextoDados db) =>
        {
            var cargo = await db.Cargos.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id);
            return cargo is null
                ? Results.NotFound(new { mensagem = "Cargo não encontrado." })
                : Results.Ok(new CargoSaida(cargo.Id, cargo.Nome, cargo.Ativo));
        });

        // POST /cargos — criar (somente Admin).
        grupo.MapPost("/", async (CargoEntrada entrada, ContextoDados db) =>
        {
            var erro = await ValidarAsync(entrada, db, idAtual: null);
            if (erro is not null) return Results.BadRequest(new { mensagem = erro });

            var cargo = new Cargo { Nome = entrada.Nome!.Trim() };
            db.Cargos.Add(cargo);
            await db.SaveChangesAsync();

            return Results.Created($"/cargos/{cargo.Id}",
                new CargoSaida(cargo.Id, cargo.Nome, cargo.Ativo));
        })
        .RequireAuthorization(p => p.RequireRole("Admin"));

        // PUT /cargos/{id} — editar (somente Admin).
        grupo.MapPut("/{id:int}", async (int id, CargoEntrada entrada, ContextoDados db) =>
        {
            var cargo = await db.Cargos.FirstOrDefaultAsync(c => c.Id == id);
            if (cargo is null) return Results.NotFound(new { mensagem = "Cargo não encontrado." });

            var erro = await ValidarAsync(entrada, db, idAtual: id);
            if (erro is not null) return Results.BadRequest(new { mensagem = erro });

            cargo.Nome = entrada.Nome!.Trim();
            await db.SaveChangesAsync();

            return Results.Ok(new CargoSaida(cargo.Id, cargo.Nome, cargo.Ativo));
        })
        .RequireAuthorization(p => p.RequireRole("Admin"));

        // DELETE /cargos/{id} — inativar (soft delete; só Admin).
        grupo.MapDelete("/{id:int}", async (int id, ContextoDados db) =>
        {
            var cargo = await db.Cargos.FirstOrDefaultAsync(c => c.Id == id);
            if (cargo is null) return Results.NotFound(new { mensagem = "Cargo não encontrado." });

            db.Cargos.Remove(cargo); // soft delete no contexto
            await db.SaveChangesAsync();
            return Results.NoContent();
        })
        .RequireAuthorization(p => p.RequireRole("Admin"));

        // POST /cargos/{id}/reativar — desfaz o soft delete.
        grupo.MapPost("/{id:int}/reativar", async (int id, ContextoDados db) =>
        {
            var cargo = await db.Cargos
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(c => c.Id == id);
            if (cargo is null) return Results.NotFound(new { mensagem = "Cargo não encontrado." });

            cargo.Ativo = true;
            await db.SaveChangesAsync();
            return Results.Ok(new CargoSaida(cargo.Id, cargo.Nome, cargo.Ativo));
        })
        .RequireAuthorization(p => p.RequireRole("Admin"));
    }

    private static async Task<string?> ValidarAsync(
        CargoEntrada entrada, ContextoDados db, int? idAtual)
    {
        if (string.IsNullOrWhiteSpace(entrada.Nome))
            return "O nome é obrigatório.";

        var nome = entrada.Nome.Trim().ToLower();
        var duplicado = await db.Cargos
            .IgnoreQueryFilters()
            .AnyAsync(c => c.Nome.ToLower() == nome && c.Id != idAtual);

        return duplicado ? "Já existe um cargo com este nome." : null;
    }
}

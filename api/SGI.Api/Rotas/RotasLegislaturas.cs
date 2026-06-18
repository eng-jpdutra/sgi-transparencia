using Microsoft.EntityFrameworkCore;
using SGI.Api.Contratos;
using SGI.Api.Contratos.Legislaturas;
using SGI.Api.Dominio;
using SGI.Api.Persistencia;

namespace SGI.Api.Rotas;

/// <summary>
/// Rotas do domínio Legislaturas.
///
/// Cadastro TOTALMENTE automático e sequencial: o usuário não informa
/// nada. A cada nova legislatura, o sistema deriva número e ano a
/// partir da última existente (a próxima começa no ano seguinte ao
/// fim da anterior). Por isso não há edição: a sequência é a regra.
///
/// Endpoints:
///   GET    /legislaturas         -> listagem paginada + filtros
///   GET    /legislaturas/proxima -> PREVIEW do que será criado (não grava)
///   GET    /legislaturas/{id}    -> um registro
///   POST   /legislaturas         -> cria a próxima (sem corpo; só Admin)
///   DELETE /legislaturas/{id}    -> inativa (soft delete; só Admin)
///
/// RBAC assimétrico: ler exige autenticação; escrever exige Admin.
/// </summary>
public static class RotasLegislaturas
{
    private const int TamanhoMaximoPagina = 100;

    // Duração fixa de uma legislatura, em anos.
    private const int DuracaoAnos = 4;

    public static void MapearRotasLegislaturas(this WebApplication app)
    {
        var grupo = app.MapGroup("/legislaturas").RequireAuthorization();

        // ==============================================================
        // GET /legislaturas — listagem paginada com filtros server-side.
        // ==============================================================
        grupo.MapGet("/", async (
            ContextoDados db,
            int pagina = 1,
            int tamanhoPagina = 20,
            int? ano = null,
            string? ordenarPor = null,
            bool descendente = false,
            bool incluirInativos = false) =>
        {
            if (pagina < 1) pagina = 1;
            if (tamanhoPagina < 1) tamanhoPagina = 20;
            if (tamanhoPagina > TamanhoMaximoPagina)
                tamanhoPagina = TamanhoMaximoPagina;

            var consulta = db.Legislaturas.AsNoTracking();

            if (incluirInativos)
                consulta = consulta.IgnoreQueryFilters();

            if (ano is not null)
                consulta = consulta.Where(l => l.AnoInicio == ano);

            var total = await consulta.CountAsync();

            // ORDENAÇÃO server-side com lista branca. Campos derivados
            // (nome, datas) mapeiam para o campo-fonte: como nome, datas
            // e número crescem juntos com AnoInicio/Numero, ordenar por
            // qualquer um deles equivale a ordenar pela fonte. Padrão:
            // número decrescente (a legislatura mais recente no topo).
            consulta = (ordenarPor?.ToLower(), descendente) switch
            {
                ("numero", false)     => consulta.OrderBy(l => l.Numero),
                ("anoinicio", false)  => consulta.OrderBy(l => l.AnoInicio),
                ("anoinicio", true)   => consulta.OrderByDescending(l => l.AnoInicio),
                ("datainicio", false) => consulta.OrderBy(l => l.AnoInicio),
                ("datainicio", true)  => consulta.OrderByDescending(l => l.AnoInicio),
                ("datafim", false)    => consulta.OrderBy(l => l.AnoInicio),
                ("datafim", true)     => consulta.OrderByDescending(l => l.AnoInicio),
                ("nome", false)       => consulta.OrderBy(l => l.AnoInicio),
                ("nome", true)        => consulta.OrderByDescending(l => l.AnoInicio),
                ("ativo", false)      => consulta.OrderBy(l => l.Ativo),
                ("ativo", true)       => consulta.OrderByDescending(l => l.Ativo),
                ("numero", true)      => consulta.OrderByDescending(l => l.Numero),
                _                     => consulta.OrderByDescending(l => l.Numero), // padrão
            };

            var entidades = await consulta
                .Skip((pagina - 1) * tamanhoPagina)
                .Take(tamanhoPagina)
                .ToListAsync();

            var itens = entidades.Select(ParaSaida).ToList();

            return Results.Ok(new ResultadoPaginado<LegislaturaSaida>(
                itens, total, pagina, tamanhoPagina));
        });

        // ==============================================================
        // GET /legislaturas/proxima — calcula (SEM gravar) qual seria a
        // próxima legislatura. Alimenta a tela de confirmação.
        // ==============================================================
        grupo.MapGet("/proxima", async (ContextoDados db) =>
        {
            var (numero, anoInicio) = await CalcularProximaAsync(db);

            // Monta um preview usando a própria lógica de derivação da
            // entidade (sem persistir) — fonte única da regra de nome/datas.
            var molde = new Legislatura { Numero = numero, AnoInicio = anoInicio };

            return Results.Ok(new ProximaLegislatura(
                molde.Numero, molde.AnoInicio, molde.AnoFim, molde.Nome,
                molde.DataInicio, molde.DataFim));
        });

        // ==============================================================
        // GET /legislaturas/{id}
        // ==============================================================
        grupo.MapGet("/{id:int}", async (int id, ContextoDados db) =>
        {
            var legislatura = await db.Legislaturas
                .AsNoTracking()
                .FirstOrDefaultAsync(l => l.Id == id);

            return legislatura is null
                ? Results.NotFound(new { mensagem = "Legislatura não encontrada." })
                : Results.Ok(ParaSaida(legislatura));
        });

        // ==============================================================
        // POST /legislaturas — cria a PRÓXIMA (sem corpo; somente Admin).
        // O número e o ano são calculados; o cliente não envia nada.
        // ==============================================================
        grupo.MapPost("/", async (ContextoDados db) =>
        {
            var (numero, anoInicio) = await CalcularProximaAsync(db);

            var legislatura = new Legislatura
            {
                Numero = numero,
                AnoInicio = anoInicio,
            };

            db.Legislaturas.Add(legislatura);
            await db.SaveChangesAsync();

            return Results.Created(
                $"/legislaturas/{legislatura.Id}", ParaSaida(legislatura));
        })
        .RequireAuthorization(politica => politica.RequireRole("Admin"));

        // ==============================================================
        // DELETE /legislaturas/{id} — inativar (soft delete; só Admin).
        // ==============================================================
        grupo.MapDelete("/{id:int}", async (int id, ContextoDados db) =>
        {
            var legislatura = await db.Legislaturas
                .FirstOrDefaultAsync(l => l.Id == id);

            if (legislatura is null)
                return Results.NotFound(new { mensagem = "Legislatura não encontrada." });

            db.Legislaturas.Remove(legislatura); // vira soft delete no contexto
            await db.SaveChangesAsync();

            return Results.NoContent();
        })
        .RequireAuthorization(politica => politica.RequireRole("Admin"));

        // ==============================================================
        // POST /legislaturas/{id}/reativar — desfaz o soft delete.
        // Busca IgnoreQueryFilters (o filtro global esconde inativos).
        // ==============================================================
        grupo.MapPost("/{id:int}/reativar", async (int id, ContextoDados db) =>
        {
            var legislatura = await db.Legislaturas
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(l => l.Id == id);

            if (legislatura is null)
                return Results.NotFound(new { mensagem = "Legislatura não encontrada." });

            legislatura.Ativo = true;
            await db.SaveChangesAsync();

            return Results.Ok(ParaSaida(legislatura));
        })
        .RequireAuthorization(politica => politica.RequireRole("Admin"));
    }

    /// <summary>
    /// Calcula número e ano de início da PRÓXIMA legislatura, a partir
    /// da mais recente existente (considerando também inativas, para a
    /// sequência nunca se repetir). Lógica compartilhada entre o preview
    /// (/proxima) e a criação (POST) — DRY: a regra mora num lugar só.
    ///
    /// Defensive Programming: se não houver nenhuma legislatura (caso
    /// que, segundo a regra de negócio, não deve ocorrer), assume o ano
    /// corrente como ponto de partida — assim o sistema responde algo
    /// coerente em vez de quebrar.
    /// </summary>
    private static async Task<(int Numero, int AnoInicio)> CalcularProximaAsync(
        ContextoDados db)
    {
        var ultima = await db.Legislaturas
            .IgnoreQueryFilters()
            .OrderByDescending(l => l.Numero)
            .FirstOrDefaultAsync();

        if (ultima is null)
            return (1, DateTime.UtcNow.Year);

        // Próxima começa no ano seguinte ao FIM da última.
        return (ultima.Numero + 1, ultima.AnoInicio + DuracaoAnos);
    }

    private static LegislaturaSaida ParaSaida(Legislatura l) => new(
        l.Id, l.Numero, l.AnoInicio, l.AnoFim, l.Nome,
        l.DataInicio, l.DataFim, l.Ativo);
}

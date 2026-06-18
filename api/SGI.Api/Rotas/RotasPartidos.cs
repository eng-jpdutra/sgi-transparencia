using Microsoft.EntityFrameworkCore;
using SGI.Api.Contratos;
using SGI.Api.Contratos.Partidos;
using SGI.Api.Dominio;
using SGI.Api.Persistencia;

namespace SGI.Api.Rotas;

/// <summary>
/// Rotas do domínio Partidos — segue o template canônico (igual a
/// Legislaturas), aqui no formato pleno: cadastro manual e com edição.
///   GET    /partidos       -> listagem paginada + filtros server-side
///   GET    /partidos/{id}  -> um registro
///   POST   /partidos       -> criar    (somente Admin)
///   PUT    /partidos/{id}  -> editar   (somente Admin)
///   DELETE /partidos/{id}  -> inativar (soft delete; somente Admin)
///
/// RBAC assimétrico: ler exige autenticação; escrever exige Admin.
/// </summary>
public static class RotasPartidos
{
    private const int TamanhoMaximoPagina = 100;

    public static void MapearRotasPartidos(this WebApplication app)
    {
        var grupo = app.MapGroup("/partidos").RequireAuthorization();

        // ==============================================================
        // GET /partidos — listagem paginada com filtros server-side.
        // ==============================================================
        grupo.MapGet("/", async (
            ContextoDados db,
            int pagina = 1,
            int tamanhoPagina = 20,
            string? busca = null,          // filtra por sigla OU nome
            string? ordenarPor = null,     // campo de ordenação
            bool descendente = false,      // direção
            bool incluirInativos = false) =>
        {
            if (pagina < 1) pagina = 1;
            if (tamanhoPagina < 1) tamanhoPagina = 20;
            if (tamanhoPagina > TamanhoMaximoPagina)
                tamanhoPagina = TamanhoMaximoPagina;

            var consulta = db.Partidos.AsNoTracking();

            if (incluirInativos)
                consulta = consulta.IgnoreQueryFilters();

            // Filtro textual único, casando sigla OU nome — normalizado
            // com ToLower() nos dois lados (paridade SQLite/PostgreSQL).
            if (!string.IsNullOrWhiteSpace(busca))
            {
                var termo = busca.ToLower();
                consulta = consulta.Where(p =>
                    p.Sigla.ToLower().Contains(termo) ||
                    p.Nome.ToLower().Contains(termo));
            }

            var total = await consulta.CountAsync();

            // ORDENAÇÃO server-side com LISTA BRANCA (Fail Fast de
            // segurança): só ordenamos por campos PREVISTOS. Um campo
            // fora da lista cai no padrão (sigla). Nunca confiamos no
            // cliente para dizer "ordene por qualquer coluna".
            consulta = (ordenarPor?.ToLower(), descendente) switch
            {
                ("numero", false)   => consulta.OrderBy(p => p.Numero),
                ("numero", true)    => consulta.OrderByDescending(p => p.Numero),
                ("nome", false)     => consulta.OrderBy(p => p.Nome),
                ("nome", true)      => consulta.OrderByDescending(p => p.Nome),
                ("ativo", false)    => consulta.OrderBy(p => p.Ativo),
                ("ativo", true)     => consulta.OrderByDescending(p => p.Ativo),
                ("sigla", true)     => consulta.OrderByDescending(p => p.Sigla),
                _                   => consulta.OrderBy(p => p.Sigla), // padrão
            };

            var itens = await consulta
                .Skip((pagina - 1) * tamanhoPagina)
                .Take(tamanhoPagina)
                .Select(p => new PartidoSaida(
                    p.Id, p.Sigla, p.Nome, p.Numero, p.Ativo))
                .ToListAsync();

            return Results.Ok(new ResultadoPaginado<PartidoSaida>(
                itens, total, pagina, tamanhoPagina));
        });

        // ==============================================================
        // GET /partidos/{id}
        // ==============================================================
        grupo.MapGet("/{id:int}", async (int id, ContextoDados db) =>
        {
            var partido = await db.Partidos
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == id);

            return partido is null
                ? Results.NotFound(new { mensagem = "Partido não encontrado." })
                : Results.Ok(new PartidoSaida(
                    partido.Id, partido.Sigla, partido.Nome,
                    partido.Numero, partido.Ativo));
        });

        // ==============================================================
        // POST /partidos — criar (somente Admin).
        // ==============================================================
        grupo.MapPost("/", async (PartidoEntrada entrada, ContextoDados db) =>
        {
            var erro = await ValidarAsync(entrada, db, idAtual: null);
            if (erro is not null)
                return Results.BadRequest(new { mensagem = erro });

            var partido = new Partido
            {
                Sigla = entrada.Sigla!.Trim().ToUpper(), // sigla sempre maiúscula
                Nome = entrada.Nome!.Trim(),
                Numero = entrada.Numero!.Value,
            };

            db.Partidos.Add(partido);
            await db.SaveChangesAsync();

            return Results.Created(
                $"/partidos/{partido.Id}",
                new PartidoSaida(partido.Id, partido.Sigla, partido.Nome,
                    partido.Numero, partido.Ativo));
        })
        .RequireAuthorization(politica => politica.RequireRole("Admin"));

        // ==============================================================
        // PUT /partidos/{id} — editar (somente Admin).
        // ==============================================================
        grupo.MapPut("/{id:int}", async (
            int id, PartidoEntrada entrada, ContextoDados db) =>
        {
            var partido = await db.Partidos
                .FirstOrDefaultAsync(p => p.Id == id);

            if (partido is null)
                return Results.NotFound(new { mensagem = "Partido não encontrado." });

            var erro = await ValidarAsync(entrada, db, idAtual: id);
            if (erro is not null)
                return Results.BadRequest(new { mensagem = erro });

            partido.Sigla = entrada.Sigla!.Trim().ToUpper();
            partido.Nome = entrada.Nome!.Trim();
            partido.Numero = entrada.Numero!.Value;

            await db.SaveChangesAsync();

            return Results.Ok(new PartidoSaida(
                partido.Id, partido.Sigla, partido.Nome,
                partido.Numero, partido.Ativo));
        })
        .RequireAuthorization(politica => politica.RequireRole("Admin"));

        // ==============================================================
        // DELETE /partidos/{id} — inativar (soft delete; só Admin).
        // ==============================================================
        grupo.MapDelete("/{id:int}", async (int id, ContextoDados db) =>
        {
            var partido = await db.Partidos
                .FirstOrDefaultAsync(p => p.Id == id);

            if (partido is null)
                return Results.NotFound(new { mensagem = "Partido não encontrado." });

            db.Partidos.Remove(partido); // vira soft delete no contexto
            await db.SaveChangesAsync();

            return Results.NoContent();
        })
        .RequireAuthorization(politica => politica.RequireRole("Admin"));

        // POST /partidos/{id}/reativar — desfaz o soft delete.
        grupo.MapPost("/{id:int}/reativar", async (int id, ContextoDados db) =>
        {
            var partido = await db.Partidos
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(p => p.Id == id);

            if (partido is null)
                return Results.NotFound(new { mensagem = "Partido não encontrado." });

            partido.Ativo = true;
            await db.SaveChangesAsync();

            return Results.Ok(new PartidoSaida(
                partido.Id, partido.Sigla, partido.Nome, partido.Numero, partido.Ativo));
        })
        .RequireAuthorization(politica => politica.RequireRole("Admin"));
    }

    /// <summary>
    /// Validação compartilhada entre criar e editar (DRY).
    /// Retorna a mensagem de erro, ou null se válido.
    /// </summary>
    private static async Task<string?> ValidarAsync(
        PartidoEntrada entrada, ContextoDados db, int? idAtual)
    {
        if (string.IsNullOrWhiteSpace(entrada.Sigla))
            return "A sigla é obrigatória.";

        if (string.IsNullOrWhiteSpace(entrada.Nome))
            return "O nome é obrigatório.";

        if (entrada.Numero is null)
            return "O número da legenda é obrigatório.";

        // Faixa do número de legenda eleitoral brasileiro (dois dígitos).
        if (entrada.Numero < 10 || entrada.Numero > 99)
            return "O número da legenda deve estar entre 10 e 99.";

        var sigla = entrada.Sigla.Trim().ToUpper();
        var nome = entrada.Nome.Trim().ToLower();

        // Duplicidades (Fail Fast antes de violar os índices unique).
        // IgnoreQueryFilters: um inativo com mesma sigla/número também conflita.
        var conflitoSigla = await db.Partidos
            .IgnoreQueryFilters()
            .AnyAsync(p => p.Sigla.ToUpper() == sigla && p.Id != idAtual);
        if (conflitoSigla)
            return "Já existe um partido com esta sigla.";

        var conflitoNumero = await db.Partidos
            .IgnoreQueryFilters()
            .AnyAsync(p => p.Numero == entrada.Numero && p.Id != idAtual);
        if (conflitoNumero)
            return "Já existe um partido com este número.";

        var conflitoNome = await db.Partidos
            .IgnoreQueryFilters()
            .AnyAsync(p => p.Nome.ToLower() == nome && p.Id != idAtual);
        if (conflitoNome)
            return "Já existe um partido com este nome.";

        return null;
    }
}

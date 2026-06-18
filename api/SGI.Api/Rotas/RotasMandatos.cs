using Microsoft.EntityFrameworkCore;
using SGI.Api.Contratos;
using SGI.Api.Contratos.Mandatos;
using SGI.Api.Dominio;
using SGI.Api.Persistencia;
using SGI.Api.Servicos;

namespace SGI.Api.Rotas;

/// <summary>
/// Rotas do domínio Mandatos — o exercício temporal do papel vereador.
/// Espelha Vínculos, e usa o MESMO serviço de não-sobreposição, então
/// a regra cruza mandatos E vínculos da pessoa.
///
///   GET    /mandatos       -> listagem paginada + filtros
///   GET    /mandatos/{id}  -> um mandato
///   POST   /mandatos       -> criar (valida sobreposição; só Admin)
///   PUT    /mandatos/{id}  -> editar (revalida; só Admin)
///   DELETE /mandatos/{id}  -> inativar (soft delete; só Admin)
/// </summary>
public static class RotasMandatos
{
    private const int TamanhoMaximoPagina = 100;

    public static void MapearRotasMandatos(this WebApplication app)
    {
        var grupo = app.MapGroup("/mandatos").RequireAuthorization();

        // GET /mandatos — listagem paginada com filtros.
        grupo.MapGet("/", async (
            ContextoDados db,
            int pagina = 1,
            int tamanhoPagina = 20,
            int? vereadorId = null,
            int? legislaturaId = null,
            bool apenasEmCurso = false,
            bool incluirInativos = false) =>
        {
            if (pagina < 1) pagina = 1;
            if (tamanhoPagina < 1) tamanhoPagina = 20;
            if (tamanhoPagina > TamanhoMaximoPagina) tamanhoPagina = TamanhoMaximoPagina;

            var consulta = db.Mandatos
                .AsNoTracking()
                .Include(m => m.Vereador)!.ThenInclude(v => v!.Pessoa)
                .Include(m => m.Legislatura)
                .Include(m => m.Matricula)
                .AsQueryable();

            if (incluirInativos) consulta = consulta.IgnoreQueryFilters();
            if (vereadorId is not null) consulta = consulta.Where(m => m.VereadorId == vereadorId);
            if (legislaturaId is not null) consulta = consulta.Where(m => m.LegislaturaId == legislaturaId);
            if (apenasEmCurso) consulta = consulta.Where(m => m.DataFim == null);

            var total = await consulta.CountAsync();

            var mandatos = await consulta
                .OrderByDescending(m => m.DataInicio)
                .Skip((pagina - 1) * tamanhoPagina)
                .Take(tamanhoPagina)
                .ToListAsync();

            var itens = mandatos.Select(ParaSaida).ToList();

            return Results.Ok(new ResultadoPaginado<MandatoSaida>(
                itens, total, pagina, tamanhoPagina));
        });

        // GET /mandatos/{id}
        grupo.MapGet("/{id:int}", async (int id, ContextoDados db) =>
        {
            var mandato = await CarregarCompleto(db).FirstOrDefaultAsync(m => m.Id == id);
            return mandato is null
                ? Results.NotFound(new { mensagem = "Mandato não encontrado." })
                : Results.Ok(ParaSaida(mandato));
        });

        // POST /mandatos — criar (somente Admin).
        grupo.MapPost("/", async (MandatoEntrada entrada, ContextoDados db) =>
        {
            var erro = await ValidarAsync(entrada, db, idAtual: null);
            if (erro is not null) return Results.BadRequest(new { mensagem = erro });

            var mandato = new Mandato
            {
                VereadorId = entrada.VereadorId!.Value,
                LegislaturaId = entrada.LegislaturaId!.Value,
                DataInicio = entrada.DataInicio!.Value,
                DataFim = entrada.DataFim,
                // Matrícula nova a cada mandato (1:1), criada na mesma operação.
                Matricula = new Matricula { Numero = entrada.Matricula!.Trim() },
            };

            db.Mandatos.Add(mandato);
            await db.SaveChangesAsync();

            var completo = await CarregarCompleto(db).FirstAsync(m => m.Id == mandato.Id);
            return Results.Created($"/mandatos/{mandato.Id}", ParaSaida(completo));
        })
        .RequireAuthorization(p => p.RequireRole("Admin"));

        // PUT /mandatos/{id} — editar (somente Admin).
        grupo.MapPut("/{id:int}", async (int id, MandatoEntrada entrada, ContextoDados db) =>
        {
            var mandato = await db.Mandatos
                .Include(m => m.Matricula)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (mandato is null) return Results.NotFound(new { mensagem = "Mandato não encontrado." });

            var erro = await ValidarAsync(entrada, db, idAtual: id, ignorarMatriculaId: mandato.MatriculaId);
            if (erro is not null) return Results.BadRequest(new { mensagem = erro });

            mandato.VereadorId = entrada.VereadorId!.Value;
            mandato.LegislaturaId = entrada.LegislaturaId!.Value;
            mandato.DataInicio = entrada.DataInicio!.Value;
            mandato.DataFim = entrada.DataFim;
            mandato.Matricula!.Numero = entrada.Matricula!.Trim();

            await db.SaveChangesAsync();

            var completo = await CarregarCompleto(db).FirstAsync(m => m.Id == id);
            return Results.Ok(ParaSaida(completo));
        })
        .RequireAuthorization(p => p.RequireRole("Admin"));

        // DELETE /mandatos/{id} — inativar (soft delete; só Admin).
        grupo.MapDelete("/{id:int}", async (int id, ContextoDados db) =>
        {
            var mandato = await db.Mandatos.FirstOrDefaultAsync(m => m.Id == id);
            if (mandato is null) return Results.NotFound(new { mensagem = "Mandato não encontrado." });

            db.Mandatos.Remove(mandato); // soft delete no contexto
            await db.SaveChangesAsync();
            return Results.NoContent();
        })
        .RequireAuthorization(p => p.RequireRole("Admin"));
    }

    private static IQueryable<Mandato> CarregarCompleto(ContextoDados db) =>
        db.Mandatos
            .Include(m => m.Vereador)!.ThenInclude(v => v!.Pessoa)
            .Include(m => m.Legislatura)
            .Include(m => m.Matricula);

    /// <summary>Validação compartilhada entre criar e editar (DRY).</summary>
    private static async Task<string?> ValidarAsync(
        MandatoEntrada entrada, ContextoDados db, int? idAtual, int? ignorarMatriculaId = null)
    {
        if (entrada.VereadorId is null) return "O vereador é obrigatório.";
        if (entrada.LegislaturaId is null) return "A legislatura é obrigatória.";
        if (string.IsNullOrWhiteSpace(entrada.Matricula)) return "A matrícula é obrigatória.";
        if (entrada.DataInicio is null) return "A data de início é obrigatória.";

        if (entrada.DataFim is not null && entrada.DataFim <= entrada.DataInicio)
            return "A data de fim deve ser posterior à data de início.";

        // Matrícula única em TODO o sistema (espaço compartilhado com os
        // vínculos). Checagem na tabela Matriculas, com IgnoreQueryFilters
        // (número não reaproveitado) e ignorando a própria na edição.
        var numero = entrada.Matricula.Trim();
        var numeroDuplicado = await db.Matriculas
            .IgnoreQueryFilters()
            .AnyAsync(m => m.Numero == numero && m.Id != ignorarMatriculaId);
        if (numeroDuplicado) return "Já existe uma matrícula com este número.";

        var vereador = await db.Vereadores
            .FirstOrDefaultAsync(v => v.Id == entrada.VereadorId);
        if (vereador is null) return "Vereador inválido.";
        if (!await db.Legislaturas.AnyAsync(l => l.Id == entrada.LegislaturaId))
            return "Legislatura inválida.";

        // ----- REGRA DE NÃO-SOBREPOSIÇÃO (cruzada) -----
        // Mesmo serviço usado pelos vínculos: checa contra mandatos E
        // vínculos da pessoa. Um mandato não coexiste com um vínculo de
        // servidor da mesma pessoa no mesmo período.
        var erroPeriodo = await ValidadorSobreposicao.ValidarPeriodoLivreAsync(
            db, vereador.PessoaId, entrada.DataInicio.Value, entrada.DataFim,
            ignorarMandatoId: idAtual);

        return erroPeriodo;
    }

    private static MandatoSaida ParaSaida(Mandato m) => new(
        m.Id,
        m.VereadorId,
        m.Vereador!.PessoaId,
        m.Vereador.Pessoa!.NomeCompleto,
        m.Vereador.NomeLegislativo,
        m.LegislaturaId,
        m.Legislatura!.Nome,
        m.Matricula!.Numero,
        m.DataInicio,
        m.DataFim,
        m.DataFim is null, // em curso
        m.Ativo);
}

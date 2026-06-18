using Microsoft.EntityFrameworkCore;
using SGI.Api.Contratos;
using SGI.Api.Contratos.Vinculos;
using SGI.Api.Dominio;
using SGI.Api.Persistencia;
using SGI.Api.Servicos;

namespace SGI.Api.Rotas;

/// <summary>
/// Rotas do domínio Vínculos — o exercício temporal do papel servidor.
///
///   GET    /vinculos       -> listagem paginada + filtros
///   GET    /vinculos/{id}  -> um vínculo
///   POST   /vinculos       -> criar (valida sobreposição; só Admin)
///   PUT    /vinculos/{id}  -> editar (revalida; só Admin)
///   DELETE /vinculos/{id}  -> inativar (soft delete; só Admin)
///
/// REGRA CENTRAL (não-sobreposição): uma pessoa não pode ter dois
/// períodos de exercício que se cruzem no tempo — nem dois vínculos,
/// nem um vínculo e um mandato (validação cruzada, ver Sobreposicao).
/// </summary>
public static class RotasVinculos
{
    private const int TamanhoMaximoPagina = 100;

    public static void MapearRotasVinculos(this WebApplication app)
    {
        var grupo = app.MapGroup("/vinculos").RequireAuthorization();

        // GET /vinculos — listagem paginada com filtros.
        grupo.MapGet("/", async (
            ContextoDados db,
            int pagina = 1,
            int tamanhoPagina = 20,
            int? servidorId = null,
            int? pessoaId = null,
            bool apenasVigentes = false,
            bool incluirInativos = false) =>
        {
            if (pagina < 1) pagina = 1;
            if (tamanhoPagina < 1) tamanhoPagina = 20;
            if (tamanhoPagina > TamanhoMaximoPagina) tamanhoPagina = TamanhoMaximoPagina;

            var consulta = db.Vinculos
                .AsNoTracking()
                .Include(v => v.Servidor)!.ThenInclude(s => s!.Pessoa)
                .Include(v => v.Cargo)
                .Include(v => v.Regime)
                .Include(v => v.Matricula)
                .AsQueryable();

            if (incluirInativos) consulta = consulta.IgnoreQueryFilters();
            if (servidorId is not null) consulta = consulta.Where(v => v.ServidorId == servidorId);
            if (pessoaId is not null) consulta = consulta.Where(v => v.Servidor!.PessoaId == pessoaId);
            if (apenasVigentes) consulta = consulta.Where(v => v.DataFim == null);

            var total = await consulta.CountAsync();

            var vinculos = await consulta
                .OrderByDescending(v => v.DataInicio)
                .Skip((pagina - 1) * tamanhoPagina)
                .Take(tamanhoPagina)
                .ToListAsync();

            var itens = vinculos.Select(ParaSaida).ToList();

            return Results.Ok(new ResultadoPaginado<VinculoSaida>(
                itens, total, pagina, tamanhoPagina));
        });

        // GET /vinculos/{id}
        grupo.MapGet("/{id:int}", async (int id, ContextoDados db) =>
        {
            var vinculo = await CarregarCompleto(db).FirstOrDefaultAsync(v => v.Id == id);
            return vinculo is null
                ? Results.NotFound(new { mensagem = "Vínculo não encontrado." })
                : Results.Ok(ParaSaida(vinculo));
        });

        // POST /vinculos — criar (somente Admin).
        grupo.MapPost("/", async (VinculoEntrada entrada, ContextoDados db) =>
        {
            var erro = await ValidarAsync(entrada, db, idAtual: null);
            if (erro is not null) return Results.BadRequest(new { mensagem = erro });

            var vinculo = new Vinculo
            {
                ServidorId = entrada.ServidorId!.Value,
                CargoId = entrada.CargoId!.Value,
                RegimeId = entrada.RegimeId!.Value,
                DataInicio = entrada.DataInicio!.Value,
                DataFim = entrada.DataFim,
                // A matrícula nasce junto do vínculo (1:1). O EF insere as
                // duas linhas na mesma operação.
                Matricula = new Matricula { Numero = entrada.Matricula!.Trim() },
            };

            db.Vinculos.Add(vinculo);
            await db.SaveChangesAsync();

            var completo = await CarregarCompleto(db).FirstAsync(v => v.Id == vinculo.Id);
            return Results.Created($"/vinculos/{vinculo.Id}", ParaSaida(completo));
        })
        .RequireAuthorization(p => p.RequireRole("Admin"));

        // PUT /vinculos/{id} — editar (somente Admin).
        grupo.MapPut("/{id:int}", async (int id, VinculoEntrada entrada, ContextoDados db) =>
        {
            var vinculo = await db.Vinculos
                .Include(v => v.Matricula)
                .FirstOrDefaultAsync(v => v.Id == id);
            if (vinculo is null) return Results.NotFound(new { mensagem = "Vínculo não encontrado." });

            var erro = await ValidarAsync(entrada, db, idAtual: id, ignorarMatriculaId: vinculo.MatriculaId);
            if (erro is not null) return Results.BadRequest(new { mensagem = erro });

            vinculo.ServidorId = entrada.ServidorId!.Value;
            vinculo.CargoId = entrada.CargoId!.Value;
            vinculo.RegimeId = entrada.RegimeId!.Value;
            vinculo.DataInicio = entrada.DataInicio!.Value;
            vinculo.DataFim = entrada.DataFim;
            vinculo.Matricula!.Numero = entrada.Matricula!.Trim();

            await db.SaveChangesAsync();

            var completo = await CarregarCompleto(db).FirstAsync(v => v.Id == id);
            return Results.Ok(ParaSaida(completo));
        })
        .RequireAuthorization(p => p.RequireRole("Admin"));

        // DELETE /vinculos/{id} — inativar (soft delete; só Admin).
        grupo.MapDelete("/{id:int}", async (int id, ContextoDados db) =>
        {
            var vinculo = await db.Vinculos.FirstOrDefaultAsync(v => v.Id == id);
            if (vinculo is null) return Results.NotFound(new { mensagem = "Vínculo não encontrado." });

            db.Vinculos.Remove(vinculo); // soft delete no contexto
            await db.SaveChangesAsync();
            return Results.NoContent();
        })
        .RequireAuthorization(p => p.RequireRole("Admin"));
    }

    /// <summary>Inclui as navegações necessárias para montar o DTO.</summary>
    private static IQueryable<Vinculo> CarregarCompleto(ContextoDados db) =>
        db.Vinculos
            .Include(v => v.Servidor)!.ThenInclude(s => s!.Pessoa)
            .Include(v => v.Cargo)
            .Include(v => v.Regime)
            .Include(v => v.Matricula);

    /// <summary>Validação compartilhada entre criar e editar (DRY).</summary>
    private static async Task<string?> ValidarAsync(
        VinculoEntrada entrada, ContextoDados db, int? idAtual, int? ignorarMatriculaId = null)
    {
        if (entrada.ServidorId is null) return "O servidor é obrigatório.";
        if (entrada.CargoId is null) return "O cargo é obrigatório.";
        if (entrada.RegimeId is null) return "O regime de contratação é obrigatório.";
        if (string.IsNullOrWhiteSpace(entrada.Matricula)) return "A matrícula é obrigatória.";
        if (entrada.DataInicio is null) return "A data de início é obrigatória.";

        // Coerência temporal: se há fim, deve ser posterior ao início.
        if (entrada.DataFim is not null && entrada.DataFim <= entrada.DataInicio)
            return "A data de fim deve ser posterior à data de início.";

        // Matrícula única em TODO o sistema. Como vínculos e mandatos
        // compartilham o espaço de numeração, a checagem é na tabela
        // Matriculas (que concentra todos os números). IgnoreQueryFilters:
        // número não é reaproveitado nem após inativação. Na edição,
        // ignora a própria matrícula do vínculo.
        var numero = entrada.Matricula.Trim();
        var numeroDuplicado = await db.Matriculas
            .IgnoreQueryFilters()
            .AnyAsync(m => m.Numero == numero && m.Id != ignorarMatriculaId);
        if (numeroDuplicado) return "Já existe uma matrícula com este número.";

        // Referências existem?
        var servidor = await db.Servidores
            .FirstOrDefaultAsync(s => s.Id == entrada.ServidorId);
        if (servidor is null) return "Servidor inválido.";
        if (!await db.Cargos.AnyAsync(c => c.Id == entrada.CargoId))
            return "Cargo inválido.";
        if (!await db.Regimes.AnyAsync(r => r.Id == entrada.RegimeId))
            return "Regime de contratação inválido.";

        // ----- REGRA DE NÃO-SOBREPOSIÇÃO (cruzada) -----
        // Usa o serviço compartilhado, que checa contra TODOS os
        // exercícios da pessoa: outros vínculos E mandatos. Assim, um
        // vínculo de servidor não pode coexistir com um mandato de
        // vereador no mesmo período — a regra "nunca dois papéis ao
        // mesmo tempo", agora completa.
        var pessoaId = servidor.PessoaId;
        var erroPeriodo = await ValidadorSobreposicao.ValidarPeriodoLivreAsync(
            db, pessoaId, entrada.DataInicio.Value, entrada.DataFim,
            ignorarVinculoId: idAtual);

        return erroPeriodo; // null se livre; mensagem se houver conflito
    }

    private static VinculoSaida ParaSaida(Vinculo v) => new(
        v.Id,
        v.ServidorId,
        v.Servidor!.PessoaId,
        v.Servidor.Pessoa!.NomeCompleto,
        v.CargoId,
        v.Cargo!.Nome,
        v.RegimeId,
        v.Regime!.Nome,
        v.Matricula!.Numero,
        v.DataInicio,
        v.DataFim,
        v.DataFim is null, // vigente
        v.Ativo);
}

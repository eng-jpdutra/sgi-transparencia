using Microsoft.EntityFrameworkCore;
using SGI.Api.Contratos;
using SGI.Api.Contratos.Pessoas;
using SGI.Api.Dominio;
using SGI.Api.Persistencia;
using SGI.Api.Servicos;

namespace SGI.Api.Rotas;

/// <summary>
/// Rotas do domínio Pessoas — o cadastro civil único e seus papéis
/// (servidor/vereador), criados de forma UNIFICADA e TRANSACIONAL.
///
///   GET    /pessoas       -> listagem paginada + filtros + ordenação
///   GET    /pessoas/{id}  -> uma pessoa com seus papéis
///   POST   /pessoas       -> cria pessoa + fichas marcadas (transação)
///   PUT    /pessoas/{id}  -> edita dados + ajusta papéis (transação)
///   DELETE /pessoas/{id}  -> inativa (soft delete; cascata nos papéis)
///
/// RBAC assimétrico: ler exige autenticação; escrever exige Admin.
/// </summary>
public static class RotasPessoas
{
    private const int TamanhoMaximoPagina = 100;

    public static void MapearRotasPessoas(this WebApplication app)
    {
        var grupo = app.MapGroup("/pessoas").RequireAuthorization();

        // ==============================================================
        // GET /pessoas — listagem paginada.
        // ==============================================================
        grupo.MapGet("/", async (
            ContextoDados db,
            int pagina = 1,
            int tamanhoPagina = 20,
            string? busca = null,        // nome ou matrícula
            string? papel = null,        // "servidor" | "vereador" | null
            string? ordenarPor = null,
            bool descendente = false,
            bool incluirInativos = false) =>
        {
            if (pagina < 1) pagina = 1;
            if (tamanhoPagina < 1) tamanhoPagina = 20;
            if (tamanhoPagina > TamanhoMaximoPagina) tamanhoPagina = TamanhoMaximoPagina;

            var consulta = db.Pessoas
                .AsNoTracking()
                .Include(p => p.Servidor)
                .Include(p => p.Vereador)
                .AsQueryable();

            if (incluirInativos) consulta = consulta.IgnoreQueryFilters();

            if (!string.IsNullOrWhiteSpace(busca))
            {
                var termo = busca.ToLower();
                var termoCpf = Cpf.Normalizar(busca);
                // Se o termo tem dígitos, busca também por CPF (que é
                // armazenado normalizado). Sem dígitos, só por nome —
                // evita que Contains("") na coluna CPF case com tudo.
                consulta = string.IsNullOrEmpty(termoCpf)
                    ? consulta.Where(p => p.NomeCompleto.ToLower().Contains(termo))
                    : consulta.Where(p =>
                        p.NomeCompleto.ToLower().Contains(termo) ||
                        p.Cpf.Contains(termoCpf));
            }

            // Filtro por papel: quem possui a ficha correspondente.
            consulta = papel?.ToLower() switch
            {
                "servidor" => consulta.Where(p => p.Servidor != null),
                "vereador" => consulta.Where(p => p.Vereador != null),
                _ => consulta,
            };

            var total = await consulta.CountAsync();

            consulta = (ordenarPor?.ToLower(), descendente) switch
            {
                ("cpf", false) => consulta.OrderBy(p => p.Cpf),
                ("cpf", true)  => consulta.OrderByDescending(p => p.Cpf),
                ("nomecompleto", true) => consulta.OrderByDescending(p => p.NomeCompleto),
                ("ativo", false) => consulta.OrderBy(p => p.Ativo),
                ("ativo", true)  => consulta.OrderByDescending(p => p.Ativo),
                _ => consulta.OrderBy(p => p.NomeCompleto), // padrão
            };

            var pessoas = await consulta
                .Skip((pagina - 1) * tamanhoPagina)
                .Take(tamanhoPagina)
                .ToListAsync();

            var itens = pessoas.Select(ParaSaida).ToList();

            return Results.Ok(new ResultadoPaginado<PessoaSaida>(
                itens, total, pagina, tamanhoPagina));
        });

        // ==============================================================
        // GET /pessoas/servidores — lista as fichas de servidor com o
        // nome da pessoa. Útil para selecionar o servidor ao criar um
        // vínculo (e para descobrir o ServidorId nos testes).
        // ==============================================================
        grupo.MapGet("/servidores", async (ContextoDados db) =>
        {
            var servidores = await db.Servidores
                .AsNoTracking()
                .Include(s => s.Pessoa)
                .OrderBy(s => s.Pessoa!.NomeCompleto)
                .Select(s => new
                {
                    servidorId = s.Id,
                    pessoaId = s.PessoaId,
                    nome = s.Pessoa!.NomeCompleto,
                    cpf = s.Pessoa.Cpf,
                })
                .ToListAsync();

            return Results.Ok(servidores);
        });

        // ==============================================================
        // GET /pessoas/vereadores — lista as fichas de vereador com o
        // nome da pessoa e o nome legislativo. Para selecionar o
        // vereador ao criar um mandato.
        // ==============================================================
        grupo.MapGet("/vereadores", async (ContextoDados db) =>
        {
            var vereadores = await db.Vereadores
                .AsNoTracking()
                .Include(v => v.Pessoa)
                .OrderBy(v => v.Pessoa!.NomeCompleto)
                .Select(v => new
                {
                    vereadorId = v.Id,
                    pessoaId = v.PessoaId,
                    nome = v.Pessoa!.NomeCompleto,
                    nomeLegislativo = v.NomeLegislativo,
                })
                .ToListAsync();

            return Results.Ok(vereadores);
        });

        // ==============================================================
        // GET /pessoas/{id}
        // ==============================================================
        grupo.MapGet("/{id:int}", async (int id, ContextoDados db) =>
        {
            var pessoa = await db.Pessoas
                .AsNoTracking()
                .IgnoreQueryFilters()
                .Include(p => p.Servidor)
                .Include(p => p.Vereador)
                .FirstOrDefaultAsync(p => p.Id == id);

            return pessoa is null
                ? Results.NotFound(new { mensagem = "Pessoa não encontrada." })
                : Results.Ok(ParaSaida(pessoa));
        });

        // ==============================================================
        // POST /pessoas — cadastro unificado TRANSACIONAL.
        // Cria a pessoa e as fichas de papel marcadas, tudo ou nada.
        // ==============================================================
        grupo.MapPost("/", async (PessoaEntrada entrada, ContextoDados db) =>
        {
            var erro = await ValidarAsync(entrada, db, idAtual: null);
            if (erro is not null) return Results.BadRequest(new { mensagem = erro });

            // TRANSAÇÃO explícita: se qualquer passo falhar, NADA é
            // gravado (nem a pessoa, nem os papéis). Garante que nunca
            // exista uma pessoa "pela metade".
            await using var transacao = await db.Database.BeginTransactionAsync();
            try
            {
                var pessoa = new Pessoa
                {
                    NomeCompleto = entrada.NomeCompleto!.Trim(),
                    Cpf = Cpf.Normalizar(entrada.Cpf),
                };

                if (entrada.EhServidor)
                    pessoa.Servidor = new Servidor();

                if (entrada.EhVereador)
                    pessoa.Vereador = new Vereador
                    {
                        NomeLegislativo = entrada.NomeLegislativo!.Trim(),
                    };

                db.Pessoas.Add(pessoa);
                await db.SaveChangesAsync();
                await transacao.CommitAsync();

                return Results.Created($"/pessoas/{pessoa.Id}", ParaSaida(pessoa));
            }
            catch
            {
                await transacao.RollbackAsync();
                throw; // o manipulador global de erros responde 500 limpo
            }
        })
        .RequireAuthorization(p => p.RequireRole("Admin"));

        // ==============================================================
        // POST /pessoas/admissao — FLUXO DE ADMISSÃO COMPLETO.
        // Numa única transação cria: a Pessoa, a ficha do papel
        // (Servidor ou Vereador) e o exercício inicial (Vínculo ou
        // Mandato). Tudo ou nada. É o cadastro unificado que o RH usa:
        // a pessoa entra já no cargo/mandato.
        // ==============================================================
        grupo.MapPost("/admissao", async (AdmissaoEntrada entrada, ContextoDados db) =>
        {
            var erro = await ValidarAdmissaoAsync(entrada, db);
            if (erro is not null) return Results.BadRequest(new { mensagem = erro });

            var cpf = Cpf.Normalizar(entrada.Cpf);
            var numero = entrada.Matricula!.Trim();
            var tipo = entrada.Tipo!.ToLower();

            await using var transacao = await db.Database.BeginTransactionAsync();
            try
            {
                // 1) Pessoa — REAPROVEITA por CPF se já existir (ex.: servidor
                //    eleito vereador, ou vereador aprovado em concurso); senão
                //    cria. A matrícula NÃO fica na pessoa: é do exercício.
                var pessoa = await db.Pessoas.FirstOrDefaultAsync(p => p.Cpf == cpf);
                if (pessoa is null)
                {
                    pessoa = new Pessoa
                    {
                        NomeCompleto = entrada.NomeCompleto!.Trim(),
                        Cpf = cpf,
                    };
                    db.Pessoas.Add(pessoa);
                    await db.SaveChangesAsync(); // gera o PessoaId
                }

                if (tipo == "servidor")
                {
                    // 2) Ficha de servidor (reaproveita se a pessoa já tiver).
                    var servidor = await db.Servidores
                        .FirstOrDefaultAsync(s => s.PessoaId == pessoa.Id);
                    if (servidor is null)
                    {
                        servidor = new Servidor { PessoaId = pessoa.Id };
                        db.Servidores.Add(servidor);
                        await db.SaveChangesAsync(); // gera o ServidorId
                    }

                    // 3) Vínculo inicial + matrícula (1:1, criada junto).
                    db.Vinculos.Add(new Vinculo
                    {
                        ServidorId = servidor.Id,
                        CargoId = entrada.CargoId!.Value,
                        RegimeId = entrada.RegimeId!.Value,
                        Matricula = new Matricula { Numero = numero },
                        DataInicio = entrada.DataInicio!.Value,
                        DataFim = entrada.DataFim,
                    });
                }
                else // vereador
                {
                    // 2) Ficha de vereador (reaproveita se já existir).
                    var vereador = await db.Vereadores
                        .FirstOrDefaultAsync(v => v.PessoaId == pessoa.Id);
                    if (vereador is null)
                    {
                        vereador = new Vereador
                        {
                            PessoaId = pessoa.Id,
                            NomeLegislativo = entrada.NomeLegislativo!.Trim(),
                        };
                        db.Vereadores.Add(vereador);
                        await db.SaveChangesAsync(); // gera o VereadorId
                    }

                    // 3) Mandato inicial + matrícula (1:1).
                    db.Mandatos.Add(new Mandato
                    {
                        VereadorId = vereador.Id,
                        LegislaturaId = entrada.LegislaturaId!.Value,
                        Matricula = new Matricula { Numero = numero },
                        DataInicio = entrada.DataInicio!.Value,
                        DataFim = entrada.DataFim,
                    });
                }

                await db.SaveChangesAsync();
                await transacao.CommitAsync();

                // Recarrega a pessoa completa para a resposta.
                var completa = await db.Pessoas
                    .Include(p => p.Servidor)
                    .Include(p => p.Vereador)
                    .FirstAsync(p => p.Id == pessoa.Id);

                return Results.Created($"/pessoas/{pessoa.Id}", ParaSaida(completa));
            }
            catch
            {
                await transacao.RollbackAsync();
                throw; // manipulador global responde 500 limpo
            }
        })
        .RequireAuthorization(p => p.RequireRole("Admin"));
        // Marcar um papel antes ausente cria a ficha; desmarcar um
        // existente a remove (soft delete).
        // ==============================================================
        grupo.MapPut("/{id:int}", async (
            int id, PessoaEntrada entrada, ContextoDados db) =>
        {
            var pessoa = await db.Pessoas
                .Include(p => p.Servidor)
                .Include(p => p.Vereador)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (pessoa is null)
                return Results.NotFound(new { mensagem = "Pessoa não encontrada." });

            var erro = await ValidarAsync(entrada, db, idAtual: id);
            if (erro is not null) return Results.BadRequest(new { mensagem = erro });

            await using var transacao = await db.Database.BeginTransactionAsync();
            try
            {
                pessoa.NomeCompleto = entrada.NomeCompleto!.Trim();
                pessoa.Cpf = Cpf.Normalizar(entrada.Cpf);

                // ----- Ficha de servidor -----
                if (entrada.EhServidor && pessoa.Servidor is null)
                    pessoa.Servidor = new Servidor();
                else if (!entrada.EhServidor && pessoa.Servidor is not null)
                    db.Servidores.Remove(pessoa.Servidor); // soft delete

                // ----- Ficha de vereador -----
                if (entrada.EhVereador)
                {
                    if (pessoa.Vereador is null)
                        pessoa.Vereador = new Vereador
                        {
                            NomeLegislativo = entrada.NomeLegislativo!.Trim(),
                        };
                    else
                        pessoa.Vereador.NomeLegislativo = entrada.NomeLegislativo!.Trim();
                }
                else if (pessoa.Vereador is not null)
                {
                    db.Vereadores.Remove(pessoa.Vereador); // soft delete
                }

                await db.SaveChangesAsync();
                await transacao.CommitAsync();

                return Results.Ok(ParaSaida(pessoa));
            }
            catch
            {
                await transacao.RollbackAsync();
                throw;
            }
        })
        .RequireAuthorization(p => p.RequireRole("Admin"));

        // ==============================================================
        // DELETE /pessoas/{id} — inativar (soft delete).
        // ==============================================================
        grupo.MapDelete("/{id:int}", async (int id, ContextoDados db) =>
        {
            var pessoa = await db.Pessoas
                .Include(p => p.Servidor)
                .Include(p => p.Vereador)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (pessoa is null)
                return Results.NotFound(new { mensagem = "Pessoa não encontrada." });

            // Inativar a pessoa inativa também as fichas de papel
            // (mantém coerência: ficha órfã de pessoa inativa não faz sentido).
            db.Pessoas.Remove(pessoa);
            if (pessoa.Servidor is not null) db.Servidores.Remove(pessoa.Servidor);
            if (pessoa.Vereador is not null) db.Vereadores.Remove(pessoa.Vereador);

            await db.SaveChangesAsync();
            return Results.NoContent();
        })
        .RequireAuthorization(p => p.RequireRole("Admin"));
    }

    /// <summary>Validação compartilhada entre criar e editar (DRY).</summary>
    private static async Task<string?> ValidarAsync(
        PessoaEntrada entrada, ContextoDados db, int? idAtual)
    {
        if (string.IsNullOrWhiteSpace(entrada.NomeCompleto))
            return "O nome completo é obrigatório.";

        if (!Cpf.EhValido(entrada.Cpf))
            return "CPF inválido.";

        // nome_legislativo é obrigatório quando há ficha de vereador.
        if (entrada.EhVereador && string.IsNullOrWhiteSpace(entrada.NomeLegislativo))
            return "O nome legislativo é obrigatório para vereadores.";

        // CPF único (Fail Fast antes da constraint), comparado normalizado.
        var cpf = Cpf.Normalizar(entrada.Cpf);
        var duplicado = await db.Pessoas
            .IgnoreQueryFilters()
            .AnyAsync(p => p.Cpf == cpf && p.Id != idAtual);
        if (duplicado)
            return "Já existe uma pessoa com este CPF.";

        return null;
    }

    /// <summary>
    /// Validação do fluxo de admissão completo. Confere os dados civis,
    /// o tipo de papel, os campos do exercício correspondente, e a
    /// regra de sobreposição (embora numa pessoa nova não haja como
    /// haver conflito ainda, mantemos a checagem por robustez e
    /// simetria com os módulos de Vínculo/Mandato).
    /// </summary>
    private static async Task<string?> ValidarAdmissaoAsync(
        AdmissaoEntrada e, ContextoDados db)
    {
        // ----- Dados civis -----
        if (string.IsNullOrWhiteSpace(e.NomeCompleto))
            return "O nome completo é obrigatório.";
        if (!Cpf.EhValido(e.Cpf))
            return "CPF inválido.";

        // ----- Matrícula DO EXERCÍCIO (única em todo o sistema) -----
        if (string.IsNullOrWhiteSpace(e.Matricula))
            return "A matrícula é obrigatória.";
        var numero = e.Matricula.Trim();
        if (await db.Matriculas.IgnoreQueryFilters().AnyAsync(m => m.Numero == numero))
            return "Já existe uma matrícula com este número.";

        // ----- Tipo de papel -----
        var tipo = e.Tipo?.ToLower();
        if (tipo != "servidor" && tipo != "vereador")
            return "Informe o tipo: servidor ou vereador.";

        // ----- Período (comum) -----
        if (e.DataInicio is null)
            return "A data de início do exercício é obrigatória.";
        if (e.DataFim is not null && e.DataFim <= e.DataInicio)
            return "A data de fim deve ser posterior à data de início.";

        // ----- Campos específicos do papel -----
        if (tipo == "servidor")
        {
            if (e.CargoId is null) return "O cargo é obrigatório.";
            if (e.RegimeId is null) return "O regime de contratação é obrigatório.";
            if (!await db.Cargos.AnyAsync(c => c.Id == e.CargoId))
                return "Cargo inválido.";
            if (!await db.Regimes.AnyAsync(r => r.Id == e.RegimeId))
                return "Regime de contratação inválido.";
        }
        else // vereador
        {
            if (string.IsNullOrWhiteSpace(e.NomeLegislativo))
                return "O nome legislativo é obrigatório para vereadores.";
            if (e.LegislaturaId is null) return "A legislatura é obrigatória.";
            if (!await db.Legislaturas.AnyAsync(l => l.Id == e.LegislaturaId))
                return "Legislatura inválida.";
        }

        // ----- Reaproveitamento de pessoa + não-sobreposição -----
        // Se já existe pessoa com este CPF, o novo exercício será ANEXADO
        // a ela. Aí a regra de não-sobreposição passa a importar: o
        // período não pode coincidir com um vínculo/mandato já existente.
        var cpf = Cpf.Normalizar(e.Cpf);
        var pessoa = await db.Pessoas.IgnoreQueryFilters()
            .FirstOrDefaultAsync(p => p.Cpf == cpf);
        if (pessoa is not null)
        {
            if (!pessoa.Ativo)
                return "Existe uma pessoa inativa com este CPF. Reative-a antes de admitir.";

            var erroPeriodo = await ValidadorSobreposicao.ValidarPeriodoLivreAsync(
                db, pessoa.Id, e.DataInicio.Value, e.DataFim);
            if (erroPeriodo is not null) return erroPeriodo;
        }

        return null;
    }
    private static PessoaSaida ParaSaida(Pessoa p) => new(
        p.Id,
        p.NomeCompleto,
        p.Cpf,
        p.Ativo,
        p.Servidor is null ? null : new PapelSaida(p.Servidor.Id, p.Servidor.Ativo),
        p.Vereador is null ? null
            : new VereadorSaida(p.Vereador.Id, p.Vereador.Ativo, p.Vereador.NomeLegislativo));
}

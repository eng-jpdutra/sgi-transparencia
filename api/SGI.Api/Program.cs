// =====================================================================
// SGI — Sistema de Gestão Integrada
// Program.cs — ponto de entrada e ÚNICA visão geral do pipeline.
//
// Regra de governança (diretrizes v2.1): este arquivo permanece
// DECLARATIVO e ENXUTO. Ele apenas "monta" a aplicação:
//   1ª metade -> registra SERVIÇOS (injeção de dependência)
//   2ª metade -> monta o PIPELINE (ordem dos middlewares) e as ROTAS
//
// Nenhuma regra de negócio vive aqui. As rotas de cada domínio
// viverão na pasta Rotas/, plugadas por extension methods
// (ex.: app.MapearRotasAutenticacao()), a partir da Etapa 3.
// =====================================================================

// "using" = importação dos namespaces utilizados neste arquivo.
using System.Text;                                      // Encoding (chave JWT)
using System.Threading.RateLimiting;                    // rate limiting
using Microsoft.AspNetCore.Authentication.JwtBearer;    // esquema Bearer
using Microsoft.EntityFrameworkCore;                    // UseSqlite, AddDbContext
using Microsoft.IdentityModel.Tokens;                   // validação do token
using SGI.Api.Infraestrutura;                           // ManipuladorErrosGlobal
using SGI.Api.Persistencia;                             // ContextoDados, SemeadorDados
using SGI.Api.Rotas;                                    // MapearRotasAutenticacao
using SGI.Api.Servicos;                                 // ServicoToken

// ---------------------------------------------------------------------
// FASE 1: O "builder" — configuração e registro de serviços.
// Tudo ANTES de builder.Build() é preparação: ainda não há servidor.
// ---------------------------------------------------------------------
var builder = WebApplication.CreateBuilder(args);
// O CreateBuilder já faz muita coisa sozinho:
//   - Lê appsettings.json e appsettings.{Ambiente}.json
//   - Lê variáveis de ambiente (é assim que segredos entram em PROD)
//   - Configura logging para o console
//   - Prepara o servidor web Kestrel

// ---------------------------------------------------------------------
// [ETAPA 2] Persistência: registro do ContextoDados (EF Core).
// ---------------------------------------------------------------------
// FAIL FAST: se a connection string não existir na configuração,
// a aplicação se RECUSA a subir, com mensagem clara — em vez de
// subir "saudável" e explodir misteriosamente na primeira query.
var connectionString = builder.Configuration.GetConnectionString("BancoSgi")
    ?? throw new InvalidOperationException(
        "Connection string 'BancoSgi' não encontrada na configuração. " +
        "Em DEV ela vem do appsettings.Development.json; em PROD, da " +
        "variável de ambiente ConnectionStrings__BancoSgi.");

// AddDbContext registra o contexto na injeção de dependência com
// tempo de vida "scoped": UMA instância por requisição HTTP —
// cada requisição enxerga o banco de forma isolada e consistente.
//
// ESTE é o único ponto do sistema que conhece o banco concreto.
// Quando formos para produção, a troca SQLite -> PostgreSQL
// acontece AQUI (UseNpgsql, decidido pelo ambiente) e em nenhum
// outro lugar — é o agnosticismo de banco na prática.
builder.Services.AddDbContext<ContextoDados>(opcoes =>
    opcoes.UseSqlite(connectionString));

// ---------------------------------------------------------------------
// [ETAPA 3] Segurança: Autenticação (JWT) + Autorização (RBAC).
// ---------------------------------------------------------------------
// FAIL FAST da chave: sem segredo configurado, a aplicação não sobe.
var chaveJwt = builder.Configuration["Jwt:ChaveSecreta"]
    ?? throw new InvalidOperationException(
        "Configuração 'Jwt:ChaveSecreta' ausente. Em DEV ela vem do " +
        "appsettings.Development.json; em PROD, da variável de ambiente " +
        "Jwt__ChaveSecreta.");

// AUTENTICAÇÃO: ensina o pipeline a entender "Authorization: Bearer x".
// Para CADA requisição com token, o middleware verifica TUDO isto —
// e qualquer falha resulta em 401, antes da rota sequer executar:
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opcoes =>
    {
        opcoes.TokenValidationParameters = new TokenValidationParameters
        {
            // A assinatura confere? (ninguém alterou o token)
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(chaveJwt)),

            // Foi emitido por NÓS, para o NOSSO frontend?
            ValidateIssuer = true,
            ValidIssuer = builder.Configuration["Jwt:Emissor"],
            ValidateAudience = true,
            ValidAudience = builder.Configuration["Jwt:Publico"],

            // Ainda está dentro da validade?
            ValidateLifetime = true,
            // Tolerância de relógio entre servidores. O padrão do .NET
            // é 5 MINUTOS — o que, na prática, estenderia a vida de
            // todo token em 5 min. Apertamos para 30 segundos.
            ClockSkew = TimeSpan.FromSeconds(30)
        };
    });

// AUTORIZAÇÃO: habilita o RequireAuthorization() das rotas (RBAC).
builder.Services.AddAuthorization();

// Gerador de tokens. Singleton: é só leitura de configuração + cálculo,
// sem estado por requisição — uma instância serve a aplicação inteira.
builder.Services.AddSingleton<ServicoToken>();

// ---------------------------------------------------------------------
// [ETAPA 5] Proteção de fronteira: erros globais, CORS e Rate Limiting.
// ---------------------------------------------------------------------

// ----- Tratamento global de erros -----
// AddProblemDetails: habilita o formato padrão RFC 7807 para erros.
// AddExceptionHandler: registra nossa última linha de defesa.
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ManipuladorErrosGlobal>();

// ----- CORS (Cross-Origin Resource Sharing) -----
// O navegador BLOQUEIA por padrão chamadas de uma origem (o React em
// localhost:5173) para outra (esta API em localhost:5180). O CORS é a
// API declarando: "estas origens específicas podem me chamar".
// A lista vem da CONFIGURAÇÃO: localhost no Development.json em DEV;
// o domínio real, via variável de ambiente, em PROD. Diretriz v2.1:
// lista explícita SEMPRE — AllowAnyOrigin é proibido.
var origensPermitidas = builder.Configuration
    .GetSection("Cors:OrigensPermitidas").Get<string[]>() ?? [];

builder.Services.AddCors(opcoes =>
    opcoes.AddPolicy("PoliticaPadrao", politica =>
        politica.WithOrigins(origensPermitidas)
                // Só os cabeçalhos que o nosso frontend de fato usa:
                .WithHeaders("Content-Type", "Authorization")
                // Só os verbos que a nossa API de fato expõe:
                .WithMethods("GET", "POST", "PUT", "DELETE")));

// ----- Rate Limiting -----
// Camada anti-abuso por VOLUME, complementar ao lockout (Etapa 4):
// o lockout protege UMA CONTA contra adivinhação de senha; o rate
// limit protege OS ENDPOINTS contra metralhadoras de requisições.
builder.Services.AddRateLimiter(opcoes =>
{
    // Quem estoura o limite recebe 429 (Too Many Requests)...
    opcoes.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    // ...com uma mensagem amigável no padrão da casa.
    opcoes.OnRejected = async (contexto, cancelamento) =>
    {
        contexto.HttpContext.Response.ContentType = "application/json";
        await contexto.HttpContext.Response.WriteAsync(
            """{"mensagem":"Limite de requisições excedido. Aguarde um instante e tente novamente."}""",
            cancelamento);
    };

    // LIMITE GLOBAL: vale para TODA rota, particionado POR IP —
    // cada endereço de origem tem sua própria cota; um cliente
    // abusivo não consome o limite dos demais.
    opcoes.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(
        contexto => RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: contexto.Connection.RemoteIpAddress?.ToString()
                          ?? "origem-desconhecida",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = builder.Configuration
                    .GetValue("Seguranca:RequisicoesPorMinuto", 100),
                Window = TimeSpan.FromMinutes(1)
            }));

    // POLÍTICA "autenticacao": cota bem mais apertada, aplicada às
    // rotas de login/renovação (são as portas que atacantes batem).
    opcoes.AddPolicy("autenticacao", contexto =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: contexto.Connection.RemoteIpAddress?.ToString()
                          ?? "origem-desconhecida",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = builder.Configuration
                    .GetValue("Seguranca:TentativasAutenticacaoPorMinuto", 10),
                Window = TimeSpan.FromMinutes(1)
            }));
});

// ---------------------------------------------------------------------
// FASE 2: O "app" — pipeline de middlewares e rotas.
// A ORDEM dos middlewares importa: cada requisição atravessa
// esta lista de cima para baixo (e a resposta volta de baixo p/ cima).
// ---------------------------------------------------------------------
var app = builder.Build();

// ---------------------------------------------------------------------
// [ETAPA 3] Preparação do banco — SOMENTE em desenvolvimento.
// A cada partida: aplica migrations pendentes e roda o seed
// (idempotente) do perfil Admin + usuário inicial.
// Em PRODUÇÃO isso NÃO acontece automaticamente: lá, migration é um
// passo deliberado e auditado do deploy — nunca um efeito colateral
// de a aplicação ter reiniciado.
// ---------------------------------------------------------------------
if (app.Environment.IsDevelopment())
{
    // "Escopo": como o ContextoDados é registrado por requisição e
    // aqui estamos FORA de uma requisição, criamos um escopo manual.
    using var escopo = app.Services.CreateScope();
    var db = escopo.ServiceProvider.GetRequiredService<ContextoDados>();

    await db.Database.MigrateAsync();      // aplica migrations pendentes
    await SemeadorDados.SemearAsync(db);   // semeia Admin (idempotente)
}

// ---------------------------------------------------------------------
// [ETAPA 5] PIPELINE COMPLETO — a ordem conta a história da requisição:
//   1º Erros:        abraça TUDO abaixo; qualquer exceção termina nele.
//   2º CORS:         barra origens não autorizadas na porta.
//   3º RateLimiter:  barra excesso de volume ANTES de gastar CPU
//                    com validação de token ou banco.
//   4º Autenticação: identifica QUEM é (lê o JWT).
//   5º Autorização:  decide O QUE esse alguém pode (RBAC).
// ---------------------------------------------------------------------
app.UseExceptionHandler();
app.UseCors("PoliticaPadrao");
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

// Rota de teste do tratamento de erros — EXISTE SOMENTE EM DEV.
// Lança uma exceção de propósito para você comprovar que o cliente
// recebe um 500 limpo enquanto o stack trace fica nos logs do servidor.
if (app.Environment.IsDevelopment())
{
    app.MapGet("/dev/erro", () =>
    {
        throw new InvalidOperationException(
            "Explosão proposital para testar o ManipuladorErrosGlobal.");
    });
}

// ---------------------------------------------------------------------
// ROTAS
// ---------------------------------------------------------------------

// Endpoint de saúde ("health check").
// É deliberadamente PÚBLICO e SEM dependências: orquestradores,
// load balancers e ferramentas de monitoramento o consultam para
// saber se a aplicação está de pé — antes mesmo de existir login.
// Responde 200 OK com um JSON mínimo e a hora do servidor em UTC
// (datas no backend são SEMPRE UTC; o fuso é problema da tela).
app.MapGet("/saude", () => Results.Ok(new
{
    situacao = "saudavel",
    horarioUtc = DateTime.UtcNow
}));

// Módulos de rotas — uma linha por domínio; o detalhe vive em Rotas/.
app.MapearRotasAutenticacao();

// [ETAPAS FUTURAS] app.MapearRotasPessoas(); app.MapearRotasServidores(); ...

// ---------------------------------------------------------------------
// FASE 3: Liga o servidor. Bloqueia aqui até a aplicação ser encerrada.
// ---------------------------------------------------------------------
app.Run();

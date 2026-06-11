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

// [ETAPA 2] Registro do ContextoDados (EF Core) entrará aqui:
//           SQLite em desenvolvimento, PostgreSQL em produção,
//           decidido pela configuração — nunca por código condicional
//           espalhado pela aplicação.

// [ETAPA 3] Autenticação JWT + Autorização (RBAC) entrarão aqui.

// [ETAPA 5] Rate Limiting e política de CORS entrarão aqui.

// ---------------------------------------------------------------------
// FASE 2: O "app" — pipeline de middlewares e rotas.
// A ORDEM dos middlewares importa: cada requisição atravessa
// esta lista de cima para baixo (e a resposta volta de baixo p/ cima).
// ---------------------------------------------------------------------
var app = builder.Build();

// [ETAPA 5] Tratamento global de erros será o PRIMEIRO middleware:
//           qualquer exceção não tratada vira uma resposta 500 limpa
//           (Results.Problem), sem vazar stack trace ao cliente.

// [ETAPA 3] app.UseAuthentication() e app.UseAuthorization()
//           entrarão aqui, NESTA ordem (primeiro identifica QUEM é,
//           depois verifica O QUE pode fazer).

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

// [ETAPA 3+] As rotas de domínio serão plugadas aqui:
//            app.MapearRotasAutenticacao();
//            app.MapearRotasPessoas();
//            ... uma linha por domínio, e só isso.

// ---------------------------------------------------------------------
// FASE 3: Liga o servidor. Bloqueia aqui até a aplicação ser encerrada.
// ---------------------------------------------------------------------
app.Run();

using Microsoft.EntityFrameworkCore;
using SGI.Api.Contratos.Usuarios;
using SGI.Api.Persistencia;

namespace SGI.Api.Rotas;

/// <summary>
/// Rotas de Perfis. Por ora, apenas LISTAGEM — para alimentar o
/// seletor de perfis no formulário de usuários. O cadastro de perfis
/// em si (criar papéis novos) pode vir depois; hoje os perfis nascem
/// do seed. Exclusivo de Admin.
/// </summary>
public static class RotasPerfis
{
    public static void MapearRotasPerfis(this WebApplication app)
    {
        var grupo = app.MapGroup("/perfis")
            .RequireAuthorization(p => p.RequireRole("Admin"));

        // GET /perfis — todos os perfis ativos (lista curta, sem paginar).
        grupo.MapGet("/", async (ContextoDados db) =>
        {
            var perfis = await db.Perfis
                .AsNoTracking()
                .OrderBy(p => p.Nome)
                .Select(p => new PerfilSaida(p.Id, p.Nome, p.Descricao))
                .ToListAsync();

            return Results.Ok(perfis);
        });
    }
}

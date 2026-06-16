using Microsoft.EntityFrameworkCore;
using SGI.Api.Dominio;
using SGI.Api.Dominio.Autenticacao;

namespace SGI.Api.Persistencia;

/// <summary>
/// Semeia os dados mínimos sem os quais o sistema nasce trancado
/// por fora: o perfil Admin e o primeiro usuário administrador.
///
/// Propriedade essencial: IDEMPOTÊNCIA. Rodar 1 ou 100 vezes produz
/// o mesmo resultado — cada bloco verifica se já existe antes de criar.
/// Por isso é seguro executá-lo a cada partida da aplicação (em DEV).
/// </summary>
public static class SemeadorDados
{
    public static async Task SemearAsync(ContextoDados db)
    {
        // ---------- Perfil Admin ----------------------------------
        var perfilAdmin = await db.Perfis
            .FirstOrDefaultAsync(p => p.Nome == "Admin");

        if (perfilAdmin is null)
        {
            perfilAdmin = new Perfil
            {
                Nome = "Admin",
                Descricao = "Acesso total ao sistema, incluindo administração " +
                            "de usuários, perfis e registros inativos."
            };
            db.Perfis.Add(perfilAdmin);
            await db.SaveChangesAsync();
        }

        // ---------- Perfil Consulta (não-admin) -------------------
        // Existe para testar o RBAC: um usuário só com este perfil
        // enxerga os módulos, mas não pode escrever.
        var perfilConsulta = await db.Perfis
            .FirstOrDefaultAsync(p => p.Nome == "Consulta");
        if (perfilConsulta is null)
        {
            perfilConsulta = new Perfil
            {
                Nome = "Consulta",
                Descricao = "Acesso somente leitura aos módulos do sistema."
            };
            db.Perfis.Add(perfilConsulta);
            await db.SaveChangesAsync();
        }

        // ---------- Usuário administrador inicial ------------------
        var existeAdmin = await db.Usuarios.AnyAsync(u => u.Login == "admin");
        if (!existeAdmin)
        {
            var usuarioAdmin = new Usuario
            {
                Login = "admin",

                // HashPassword: gera um "sal" aleatório, aplica o BCrypt
                // e devolve sal+hash juntos numa string. Dois usuários com
                // a MESMA senha terão hashes DIFERENTES (graças ao sal) —
                // o que inviabiliza ataques por tabelas pré-computadas.
                SenhaHash = BCrypt.Net.BCrypt.HashPassword("Trocar@123"),

                // Governança: senha provisória OBRIGA troca no primeiro
                // acesso. O frontend (Etapa 6) lerá este flag na resposta
                // do login e forçará o fluxo de troca.
                DeveTrocarSenha = true,

                Perfis = { new UsuarioPerfil { Perfil = perfilAdmin } }
            };

            db.Usuarios.Add(usuarioAdmin);
            await db.SaveChangesAsync();
        }

        // ---------- Usuário comum (perfil Consulta) ----------------
        // Para testar o RBAC: login "consulta", senha provisória
        // "Trocar@123". Sem perfil Admin -> não vê botões de escrita
        // e o backend recusa operações administrativas com 403.
        var existeConsulta = await db.Usuarios.AnyAsync(u => u.Login == "consulta");
        if (!existeConsulta)
        {
            var usuarioConsulta = new Usuario
            {
                Login = "consulta",
                SenhaHash = BCrypt.Net.BCrypt.HashPassword("Trocar@123"),
                DeveTrocarSenha = true,
                Perfis = { new UsuarioPerfil { Perfil = perfilConsulta } }
            };
            db.Usuarios.Add(usuarioConsulta);
            await db.SaveChangesAsync();
        }

        // ---------- Legislaturas de exemplo (facilitam testar a
        //            paginação e os filtros já no primeiro acesso) ----
        if (!await db.Legislaturas.AnyAsync())
        {
            // Informa-se apenas número e ano de início; o resto é derivado.
            db.Legislaturas.AddRange(
                new Legislatura { Numero = 19, AnoInicio = 2013 },
                new Legislatura { Numero = 20, AnoInicio = 2017 },
                new Legislatura { Numero = 21, AnoInicio = 2021 },
                new Legislatura { Numero = 22, AnoInicio = 2025 });

            await db.SaveChangesAsync();
        }

        // ---------- Partidos de exemplo ----------
        if (!await db.Partidos.AnyAsync())
        {
            db.Partidos.AddRange(
                new Partido { Sigla = "MDB", Nome = "Movimento Democrático Brasileiro", Numero = 15 },
                new Partido { Sigla = "PT", Nome = "Partido dos Trabalhadores", Numero = 13 },
                new Partido { Sigla = "PL", Nome = "Partido Liberal", Numero = 22 },
                new Partido { Sigla = "PSDB", Nome = "Partido da Social Democracia Brasileira", Numero = 45 },
                new Partido { Sigla = "PP", Nome = "Progressistas", Numero = 11 });

            await db.SaveChangesAsync();
        }

        // ---------- Cargos de exemplo ----------
        if (!await db.Cargos.AnyAsync())
        {
            db.Cargos.AddRange(
                new Cargo { Nome = "Assessor Parlamentar" },
                new Cargo { Nome = "Analista Legislativo" },
                new Cargo { Nome = "Técnico Administrativo" },
                new Cargo { Nome = "Procurador Jurídico" });

            await db.SaveChangesAsync();
        }

        // ---------- Regimes de contratação de exemplo ----------
        if (!await db.Regimes.AnyAsync())
        {
            db.Regimes.AddRange(
                new RegimeContratacao { Nome = "Efetivo" },
                new RegimeContratacao { Nome = "Comissionado" },
                new RegimeContratacao { Nome = "Temporário" });

            await db.SaveChangesAsync();
        }
    }
}

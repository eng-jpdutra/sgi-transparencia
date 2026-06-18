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
        // IgnoreQueryFilters: a verificação precisa enxergar o login
        // mesmo que o usuário esteja INATIVO. O índice UNIQUE do banco
        // vale para ativos e inativos, então checar só entre ativos
        // levaria a tentar recriar um login que já existe (erro 19).
        var existeAdmin = await db.Usuarios
            .IgnoreQueryFilters()
            .AnyAsync(u => u.Login == "admin");
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
        var existeConsulta = await db.Usuarios
            .IgnoreQueryFilters()
            .AnyAsync(u => u.Login == "consulta");
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

        // ---------- Pessoa de teste: servidora que VIROU vereadora ----------
        // Demonstra a refatoração: a matrícula é do EXERCÍCIO (não da
        // pessoa) e é única no sistema. A mesma pessoa tem DUAS matrículas
        // distintas, em exercícios SEQUENCIAIS (a regra proíbe sobreposição):
        // foi servidora (vínculo encerrado, mat. 9001) e hoje é vereadora
        // (mandato em curso, mat. 9002). CPF de teste válido: 111.444.777-35.
        if (!await db.Pessoas.IgnoreQueryFilters().AnyAsync(p => p.Cpf == "11144477735"))
        {
            var cargo = await db.Cargos.FirstAsync();
            var regime = await db.Regimes.FirstAsync();
            var legislatura = await db.Legislaturas.FirstAsync(l => l.Numero == 22);

            var pessoaTeste = new Pessoa
            {
                NomeCompleto = "Maria de Teste",
                Cpf = "11144477735",
                Servidor = new Servidor(),
                Vereador = new Vereador { NomeLegislativo = "Maria de Teste" },
            };
            db.Pessoas.Add(pessoaTeste);
            await db.SaveChangesAsync(); // gera ids da pessoa e das fichas

            // Vínculo de servidora, JÁ ENCERRADO (matrícula 9001).
            db.Vinculos.Add(new Vinculo
            {
                ServidorId = pessoaTeste.Servidor!.Id,
                CargoId = cargo.Id,
                RegimeId = regime.Id,
                Matricula = new Matricula { Numero = "9001" },
                DataInicio = new DateOnly(2017, 2, 1),
                DataFim = new DateOnly(2024, 12, 31),
            });

            // Mandato de vereadora, EM CURSO (matrícula 9002, diferente).
            db.Mandatos.Add(new Mandato
            {
                VereadorId = pessoaTeste.Vereador!.Id,
                LegislaturaId = legislatura.Id,
                Matricula = new Matricula { Numero = "9002" },
                DataInicio = new DateOnly(2025, 1, 1),
                DataFim = null,
            });

            await db.SaveChangesAsync();
        }
    }
}

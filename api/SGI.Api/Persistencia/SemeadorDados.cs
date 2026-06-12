using Microsoft.EntityFrameworkCore;
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
    }
}

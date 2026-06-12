using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using SGI.Api.Dominio;
using SGI.Api.Dominio.Autenticacao;

namespace SGI.Api.Persistencia;

/// <summary>
/// Porta de entrada ÚNICA para o banco de dados (padrão Unit of Work
/// do EF Core). Três responsabilidades de governança vivem aqui,
/// valendo automaticamente para TODA entidade, presente e futura:
///
///   1. Query filter global de soft delete (inativos somem das queries).
///   2. Preenchimento automático da auditoria (CriadoEm/AtualizadoEm).
///   3. Rede de segurança: tentativas de delete físico viram soft delete.
///
/// Repare que esta classe NÃO sabe qual banco está por trás
/// (SQLite ou PostgreSQL) — o provider é decidido no Program.cs.
/// É isso que mantém o código 100% agnóstico ao banco.
/// </summary>
public class ContextoDados : DbContext
{
    public ContextoDados(DbContextOptions<ContextoDados> opcoes) : base(opcoes) { }

    // -----------------------------------------------------------------
    // DbSets: cada um vira uma tabela. O nome da propriedade
    // vira o nome da tabela no banco (Usuarios, Perfis, ...).
    // -----------------------------------------------------------------
    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<Perfil> Perfis => Set<Perfil>();
    public DbSet<UsuarioPerfil> UsuariosPerfis => Set<UsuarioPerfil>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    /// <summary>
    /// Configuração do modelo: índices, tamanhos de coluna e o
    /// query filter global. Roda UMA vez, na inicialização.
    /// </summary>
    protected override void OnModelCreating(ModelBuilder modelo)
    {
        base.OnModelCreating(modelo);

        // --------------------------------------------------------------
        // USUARIO
        // --------------------------------------------------------------
        // Login único: é o banco quem garante que não existem dois
        // usuários com o mesmo login — a checagem no código (Fail Fast,
        // Etapa 3) é cortesia para o usuário; a LEI é a constraint.
        modelo.Entity<Usuario>().HasIndex(u => u.Login).IsUnique();
        // Tamanhos máximos: sem eles, o banco cria colunas de texto
        // ilimitado — desperdício e porta para abuso.
        modelo.Entity<Usuario>().Property(u => u.Login).HasMaxLength(100);
        modelo.Entity<Usuario>().Property(u => u.SenhaHash).HasMaxLength(100);

        // --------------------------------------------------------------
        // PERFIL
        // --------------------------------------------------------------
        modelo.Entity<Perfil>().HasIndex(p => p.Nome).IsUnique();
        modelo.Entity<Perfil>().Property(p => p.Nome).HasMaxLength(50);
        modelo.Entity<Perfil>().Property(p => p.Descricao).HasMaxLength(255);

        // --------------------------------------------------------------
        // USUARIO_PERFIL
        // --------------------------------------------------------------
        // Um usuário não pode ter o mesmo perfil atribuído duas vezes.
        modelo.Entity<UsuarioPerfil>()
              .HasIndex(up => new { up.UsuarioId, up.PerfilId })
              .IsUnique();

        // --------------------------------------------------------------
        // REFRESH_TOKEN
        // --------------------------------------------------------------
        // O hash é a chave de busca do fluxo de renovação (Etapa 4):
        // índice unique = busca instantânea + impossibilidade de colisão.
        modelo.Entity<RefreshToken>().HasIndex(rt => rt.TokenHash).IsUnique();
        modelo.Entity<RefreshToken>().Property(rt => rt.TokenHash).HasMaxLength(100);

        // --------------------------------------------------------------
        // QUERY FILTER GLOBAL DE SOFT DELETE (diretriz inegociável)
        // --------------------------------------------------------------
        // Para CADA entidade que herda EntidadeBase, montamos por
        // reflexão o filtro "e => e.Ativo" e o registramos no modelo.
        // Efeito prático: db.Usuarios.ToListAsync() traz APENAS os
        // ativos, sem que nenhum endpoint precise lembrar do Where.
        // O esquecimento humano deixa de ser um risco.
        // (Acesso a inativos, quando legítimo: IgnoreQueryFilters(),
        // restrito a rotas administrativas autorizadas.)
        foreach (var tipoEntidade in modelo.Model.GetEntityTypes())
        {
            if (!typeof(EntidadeBase).IsAssignableFrom(tipoEntidade.ClrType))
                continue;

            // Monta dinamicamente a expressão: (e) => e.Ativo
            var parametro = Expression.Parameter(tipoEntidade.ClrType, "e");
            var propriedadeAtivo = Expression.Property(parametro, nameof(EntidadeBase.Ativo));
            var filtro = Expression.Lambda(propriedadeAtivo, parametro);

            modelo.Entity(tipoEntidade.ClrType).HasQueryFilter(filtro);
        }
    }

    /// <summary>
    /// Interceptação de TODO salvamento. É aqui que a auditoria é
    /// preenchida e que o hard delete é neutralizado — de forma
    /// centralizada, invisível e impossível de esquecer (DRY).
    /// </summary>
    public override Task<int> SaveChangesAsync(CancellationToken cancelamento = default)
    {
        var agoraUtc = DateTime.UtcNow;

        // ChangeTracker = a lista do que está prestes a ser gravado.
        foreach (var entrada in ChangeTracker.Entries<EntidadeBase>())
        {
            switch (entrada.State)
            {
                case EntityState.Added:
                    // Registro novo: carimba a criação.
                    entrada.Entity.CriadoEm = agoraUtc;
                    break;

                case EntityState.Modified:
                    // Alteração: carimba a atualização e PROTEGE o
                    // CriadoEm contra sobrescrita acidental.
                    entrada.Entity.AtualizadoEm = agoraUtc;
                    entrada.Property(e => e.CriadoEm).IsModified = false;
                    break;

                case EntityState.Deleted:
                    // REDE DE SEGURANÇA: alguém chamou Remove().
                    // Em vez de deixar o DELETE chegar ao banco,
                    // convertemos a operação em UPDATE de soft delete.
                    // A diretriz deixa de depender de disciplina:
                    // o delete físico é tecnicamente impossível
                    // passando por este contexto.
                    entrada.State = EntityState.Modified;
                    entrada.Entity.Ativo = false;
                    entrada.Entity.AtualizadoEm = agoraUtc;
                    break;
            }
        }

        return base.SaveChangesAsync(cancelamento);
    }
}

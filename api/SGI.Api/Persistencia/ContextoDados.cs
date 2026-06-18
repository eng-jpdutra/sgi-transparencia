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
    public DbSet<Legislatura> Legislaturas => Set<Legislatura>();
    public DbSet<Partido> Partidos => Set<Partido>();
    public DbSet<Cargo> Cargos => Set<Cargo>();
    public DbSet<RegimeContratacao> Regimes => Set<RegimeContratacao>();
    public DbSet<Pessoa> Pessoas => Set<Pessoa>();
    public DbSet<Servidor> Servidores => Set<Servidor>();
    public DbSet<Vereador> Vereadores => Set<Vereador>();
    public DbSet<Matricula> Matriculas => Set<Matricula>();
    public DbSet<Vinculo> Vinculos => Set<Vinculo>();
    public DbSet<Mandato> Mandatos => Set<Mandato>();

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
        // LEGISLATURA
        // --------------------------------------------------------------
        // Numero e AnoInicio são únicos (a sequência ordinal não se
        // repete; o ano de início também não). São os campos filtráveis
        // /ordenáveis, logo indexados (diretriz v2.1). Nome, datas e
        // AnoFim NÃO são colunas — são derivados ([NotMapped] na entidade).
        modelo.Entity<Legislatura>().HasIndex(l => l.Numero).IsUnique();
        modelo.Entity<Legislatura>().HasIndex(l => l.AnoInicio).IsUnique();

        // --------------------------------------------------------------
        // PARTIDO
        // --------------------------------------------------------------
        // Sigla, Numero e Nome são únicos e filtráveis -> indexados
        // (diretriz v2.1: toda coluna filtrável/identificadora tem índice).
        modelo.Entity<Partido>().HasIndex(p => p.Sigla).IsUnique();
        modelo.Entity<Partido>().HasIndex(p => p.Numero).IsUnique();
        modelo.Entity<Partido>().HasIndex(p => p.Nome).IsUnique();
        modelo.Entity<Partido>().Property(p => p.Sigla).HasMaxLength(25);
        modelo.Entity<Partido>().Property(p => p.Nome).HasMaxLength(100);

        // --------------------------------------------------------------
        // CARGO e REGIME DE CONTRATAÇÃO (estrutura idêntica)
        // --------------------------------------------------------------
        modelo.Entity<Cargo>().HasIndex(c => c.Nome).IsUnique();
        modelo.Entity<Cargo>().Property(c => c.Nome).HasMaxLength(100);
        modelo.Entity<RegimeContratacao>().HasIndex(r => r.Nome).IsUnique();
        modelo.Entity<RegimeContratacao>().Property(r => r.Nome).HasMaxLength(100);

        // --------------------------------------------------------------
        // PESSOA + papéis (SERVIDOR, VEREADOR)
        // --------------------------------------------------------------
        // CPF é a chave natural civil: único (ativos+inativos), 11 dígitos
        // (armazenado normalizado), filtrável na pesquisa -> indexado.
        modelo.Entity<Pessoa>().HasIndex(p => p.Cpf).IsUnique();
        modelo.Entity<Pessoa>().Property(p => p.Cpf).HasMaxLength(11);
        modelo.Entity<Pessoa>().Property(p => p.NomeCompleto).HasMaxLength(100);

        // --------------------------------------------------------------
        // MATRICULA (registro funcional) — número ÚNICO GLOBAL
        // --------------------------------------------------------------
        // A unicidade do número vive aqui, numa tabela só, porque é uma
        // regra CRUZADA entre Vínculo e Mandato (espaço de numeração
        // compartilhado). Um índice por tabela não cobriria isso; este
        // índice único, sim. Vale para ativos+inativos (número não é
        // reaproveitado) -> checagens usam IgnoreQueryFilters().
        modelo.Entity<Matricula>().HasIndex(m => m.Numero).IsUnique();
        modelo.Entity<Matricula>().Property(m => m.Numero).HasMaxLength(10);

        // Relação 1:1 Pessoa<->Servidor. A FK PessoaId é ÚNICA, o que
        // garante no banco que uma pessoa tenha no máximo uma ficha de
        // servidor. Restrict: não apaga a pessoa em cascata pelo papel.
        modelo.Entity<Servidor>().HasIndex(s => s.PessoaId).IsUnique();
        modelo.Entity<Servidor>()
            .HasOne(s => s.Pessoa)
            .WithOne(p => p.Servidor)
            .HasForeignKey<Servidor>(s => s.PessoaId)
            .OnDelete(DeleteBehavior.Restrict);

        // Relação 1:1 Pessoa<->Vereador (mesma lógica).
        modelo.Entity<Vereador>().HasIndex(v => v.PessoaId).IsUnique();
        modelo.Entity<Vereador>().Property(v => v.NomeLegislativo).HasMaxLength(100);
        modelo.Entity<Vereador>()
            .HasOne(v => v.Pessoa)
            .WithOne(p => p.Vereador)
            .HasForeignKey<Vereador>(v => v.PessoaId)
            .OnDelete(DeleteBehavior.Restrict);

        // --------------------------------------------------------------
        // VÍNCULO (exercício temporal do servidor)
        // --------------------------------------------------------------
        // FKs para servidor, cargo e regime. Restrict em todas: não se
        // apaga um cargo/regime/servidor que tenha vínculos atrelados.
        // Índices nas FKs (são filtráveis e usados em joins).
        modelo.Entity<Vinculo>().HasIndex(v => v.ServidorId);
        modelo.Entity<Vinculo>().HasIndex(v => v.CargoId);
        modelo.Entity<Vinculo>().HasIndex(v => v.RegimeId);
        modelo.Entity<Vinculo>()
            .HasOne(v => v.Servidor).WithMany()
            .HasForeignKey(v => v.ServidorId)
            .OnDelete(DeleteBehavior.Restrict);
        modelo.Entity<Vinculo>()
            .HasOne(v => v.Cargo).WithMany()
            .HasForeignKey(v => v.CargoId)
            .OnDelete(DeleteBehavior.Restrict);
        modelo.Entity<Vinculo>()
            .HasOne(v => v.Regime).WithMany()
            .HasForeignKey(v => v.RegimeId)
            .OnDelete(DeleteBehavior.Restrict);

        // 1:1 Vínculo<->Matrícula. A FK MatriculaId no lado do vínculo é
        // única (o EF cria o índice unique automaticamente no 1:1), o que
        // garante que uma matrícula identifica no máximo um vínculo.
        // Restrict: não se apaga a matrícula em cascata pelo vínculo.
        modelo.Entity<Vinculo>()
            .HasOne(v => v.Matricula).WithOne(m => m.Vinculo)
            .HasForeignKey<Vinculo>(v => v.MatriculaId)
            .OnDelete(DeleteBehavior.Restrict);

        // --------------------------------------------------------------
        // MANDATO (exercício temporal do vereador)
        // --------------------------------------------------------------
        modelo.Entity<Mandato>().HasIndex(m => m.VereadorId);
        modelo.Entity<Mandato>().HasIndex(m => m.LegislaturaId);
        modelo.Entity<Mandato>()
            .HasOne(m => m.Vereador).WithMany()
            .HasForeignKey(m => m.VereadorId)
            .OnDelete(DeleteBehavior.Restrict);
        modelo.Entity<Mandato>()
            .HasOne(m => m.Legislatura).WithMany()
            .HasForeignKey(m => m.LegislaturaId)
            .OnDelete(DeleteBehavior.Restrict);

        // 1:1 Mandato<->Matrícula (mesma lógica do vínculo).
        modelo.Entity<Mandato>()
            .HasOne(m => m.Matricula).WithOne(mt => mt.Mandato)
            .HasForeignKey<Mandato>(m => m.MatriculaId)
            .OnDelete(DeleteBehavior.Restrict);

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

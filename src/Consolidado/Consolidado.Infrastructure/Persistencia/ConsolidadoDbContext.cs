using Consolidado.Domain;
using Consolidado.Infrastructure.Idempotencia;
using Microsoft.EntityFrameworkCore;

namespace Consolidado.Infrastructure.Persistencia;

public sealed class ConsolidadoDbContext : DbContext
{
    public ConsolidadoDbContext(DbContextOptions<ConsolidadoDbContext> options) : base(options) { }

    public DbSet<SaldoDiario> SaldosDiarios => Set<SaldoDiario>();
    internal DbSet<MensagemProcessada> MensagensProcessadas => Set<MensagemProcessada>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ConsolidadoDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}

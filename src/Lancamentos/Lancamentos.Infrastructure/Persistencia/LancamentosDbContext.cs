using System.Text.Json;
using FluxoCaixa.Domain.Primitives;
using Lancamentos.Domain;
using Lancamentos.Infrastructure.Outbox;
using Microsoft.EntityFrameworkCore;

namespace Lancamentos.Infrastructure.Persistencia;

public sealed class LancamentosDbContext : DbContext
{
    public LancamentosDbContext(DbContextOptions<LancamentosDbContext> options) : base(options) { }

    public DbSet<Lancamento> Lancamentos => Set<Lancamento>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(LancamentosDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        InterceptarEventosDeDominio();
        return await base.SaveChangesAsync(cancellationToken);
    }

    private void InterceptarEventosDeDominio()
    {
        var entidades = ChangeTracker
            .Entries<Entity>()
            .Where(e => e.Entity.DomainEvents.Count > 0)
            .ToList();

        var mensagensOutbox = entidades
            .SelectMany(e => e.Entity.DomainEvents)
            .Select(evento => new OutboxMessage
            {
                Id = Guid.CreateVersion7(),
                Tipo = evento.GetType().AssemblyQualifiedName!,
                Payload = JsonSerializer.Serialize(evento, evento.GetType()),
                CriadoEm = DateTimeOffset.UtcNow
            })
            .ToList();

        OutboxMessages.AddRange(mensagensOutbox);

        foreach (var entrada in entidades)
            entrada.Entity.ClearDomainEvents();
    }
}

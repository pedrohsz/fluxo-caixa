using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Consolidado.Infrastructure.Persistencia;

public sealed class DesignTimeConsolidadoDbContextFactory : IDesignTimeDbContextFactory<ConsolidadoDbContext>
{
    public ConsolidadoDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<ConsolidadoDbContext>()
            .UseNpgsql("Host=localhost;Port=5432;Database=consolidado;Username=postgres;Password=postgres")
            .Options;

        return new ConsolidadoDbContext(options);
    }
}

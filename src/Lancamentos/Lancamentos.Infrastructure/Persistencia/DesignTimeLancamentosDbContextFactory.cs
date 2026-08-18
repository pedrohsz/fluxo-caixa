using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Lancamentos.Infrastructure.Persistencia;

public sealed class DesignTimeLancamentosDbContextFactory : IDesignTimeDbContextFactory<LancamentosDbContext>
{
    public LancamentosDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<LancamentosDbContext>()
            .UseNpgsql("Host=localhost;Port=5432;Database=fluxocaixa;Username=postgres;Password=postgres")
            .Options;

        return new LancamentosDbContext(options);
    }
}

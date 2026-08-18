using Consolidado.Infrastructure.Persistencia;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Consolidado.Api.HealthChecks;

internal sealed class PostgresConsolidadoHealthCheck : IHealthCheck
{
    private readonly IDbContextFactory<ConsolidadoDbContext> _dbFactory;

    public PostgresConsolidadoHealthCheck(IDbContextFactory<ConsolidadoDbContext> dbFactory) =>
        _dbFactory = dbFactory;

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
            return await db.Database.CanConnectAsync(cancellationToken)
                ? HealthCheckResult.Healthy()
                : HealthCheckResult.Unhealthy("Postgres não respondeu");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Postgres inacessível", ex);
        }
    }
}

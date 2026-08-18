using Lancamentos.Infrastructure.Persistencia;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Lancamentos.Api.HealthChecks;

internal sealed class PostgresLancamentosHealthCheck : IHealthCheck
{
    private readonly IServiceScopeFactory _scopeFactory;

    public PostgresLancamentosHealthCheck(IServiceScopeFactory scopeFactory) =>
        _scopeFactory = scopeFactory;

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<LancamentosDbContext>();
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

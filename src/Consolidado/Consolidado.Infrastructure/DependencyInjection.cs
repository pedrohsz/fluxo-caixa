using Consolidado.Application.Consultas.ObterSaldoDiario;
using Consolidado.Application.Portas;
using Consolidado.Infrastructure.Consumidores;
using Consolidado.Infrastructure.Persistencia;
using Consolidado.Infrastructure.Persistencia.Repositorios;
using FluxoCaixa.Application.Abstractions;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Consolidado.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddConsolidadoInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContextFactory<ConsolidadoDbContext>(opts =>
            opts.UseNpgsql(configuration.GetConnectionString("Postgres")));

        services.AddScoped<ISaldoDiarioRepositorio, SaldoDiarioRepositorio>();
        services.AddScoped<IQueryHandler<ObterSaldoDiarioQuery, SaldoDiarioDto>, ObterSaldoDiarioQueryHandler>();
        services.AddScoped<IDispatcher, Dispatcher>();

        services.AddStackExchangeRedisCache(opts =>
            opts.Configuration = configuration.GetConnectionString("Redis"));

        var rabbitHost = configuration["RabbitMQ:Host"] ?? "rabbitmq://rabbitmq";
        var rabbitUser = configuration["RabbitMQ:Username"] ?? "guest";
        var rabbitPass = configuration["RabbitMQ:Password"] ?? "guest";

        services.AddMassTransit(x =>
        {
            x.AddConsumer<LancamentoRegistradoConsumidor>();

            x.UsingRabbitMq((ctx, cfg) =>
            {
                cfg.Host(rabbitHost, h =>
                {
                    h.Username(rabbitUser);
                    h.Password(rabbitPass);
                });
                cfg.ConfigureEndpoints(ctx);
            });
        });

        return services;
    }

    public static async Task AplicarMigracoesConsolidadoAsync(this IServiceProvider services)
    {
        for (var tentativa = 1; ; tentativa++)
        {
            try
            {
                using var scope = services.CreateScope();
                var factory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<ConsolidadoDbContext>>();
                await using var db = await factory.CreateDbContextAsync();
                await db.Database.MigrateAsync();
                return;
            }
            catch when (tentativa < 6)
            {
                await Task.Delay(TimeSpan.FromSeconds(3 * tentativa));
            }
        }
    }
}

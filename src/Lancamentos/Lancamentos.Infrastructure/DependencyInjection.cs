using FluentValidation;
using FluxoCaixa.Application.Abstractions;
using Lancamentos.Application.Comandos.RegistrarLancamento;
using Lancamentos.Application.Consultas.ObterLancamento;
using Lancamentos.Application.Portas;
using Lancamentos.Infrastructure.Outbox;
using Lancamentos.Infrastructure.Persistencia;
using Lancamentos.Infrastructure.Persistencia.Repositorios;
using Lancamentos.Infrastructure.Pipeline;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Lancamentos.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<LancamentosDbContext>(opts =>
            opts.UseNpgsql(configuration.GetConnectionString("Postgres")));

        services.AddScoped<ILancamentoRepositorio, LancamentoRepositorio>();

        services.AddValidatorsFromAssemblyContaining<RegistrarLancamentoValidator>(ServiceLifetime.Scoped);

        // Command handlers com decorator de validação
        services.AddScoped<RegistrarLancamentoCommandHandler>();
        services.AddScoped<ICommandHandler<RegistrarLancamentoCommand, Guid>>(sp =>
            new ValidationCommandDecorator<RegistrarLancamentoCommand, Guid>(
                sp.GetRequiredService<RegistrarLancamentoCommandHandler>(),
                sp.GetService<IValidator<RegistrarLancamentoCommand>>()));

        // Query handlers (sem validação — apenas ID)
        services.AddScoped<IQueryHandler<ObterLancamentoQuery, LancamentoDto>, ObterLancamentoQueryHandler>();

        services.AddScoped<IDispatcher, Dispatcher>();

        // MassTransit / RabbitMQ
        var rabbitHost = configuration["RabbitMQ:Host"] ?? "rabbitmq://rabbitmq";
        var rabbitUser = configuration["RabbitMQ:Username"] ?? "guest";
        var rabbitPass = configuration["RabbitMQ:Password"] ?? "guest";

        services.AddMassTransit(x =>
        {
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

        // Outbox relay background service
        services.AddHostedService<OutboxRelayWorker>();

        return services;
    }

    public static async Task AplicarMigracoesAsync(this IServiceProvider services)
    {
        for (var tentativa = 1; ; tentativa++)
        {
            try
            {
                using var scope = services.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<LancamentosDbContext>();
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

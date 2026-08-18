using System.Text.Json;
using Lancamentos.Infrastructure.Persistencia;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Lancamentos.Infrastructure.Outbox;

public sealed class OutboxRelayWorker : BackgroundService
{
    private static readonly TimeSpan _intervalo = TimeSpan.FromSeconds(2);
    private const int _loteTamanho = 200;

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<OutboxRelayWorker> _logger;

    public OutboxRelayWorker(
        IServiceScopeFactory scopeFactory,
        ILogger<OutboxRelayWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var processadas = await ProcessarLoteAsync(stoppingToken);

            // Só aguarda o intervalo quando o lote veio incompleto (sem backlog).
            // Se veio cheio, há mais trabalho esperando — lê imediatamente o próximo lote.
            if (processadas < _loteTamanho)
                await Task.Delay(_intervalo, stoppingToken).ConfigureAwait(false);
        }
    }

    private async Task<int> ProcessarLoteAsync(CancellationToken cancellationToken)
    {
        try
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<LancamentosDbContext>();
            var bus = scope.ServiceProvider.GetRequiredService<IPublishEndpoint>();

            var mensagens = await db.OutboxMessages
                .Where(m => m.ProcessadoEm == null && m.Erro == null)
                .OrderBy(m => m.CriadoEm)
                .Take(_loteTamanho)
                .ToListAsync(cancellationToken);

            if (mensagens.Count == 0)
                return 0;

            // Publica até 10 mensagens em paralelo. Cada tarefa opera em sua própria
            // instância de mensagem — não há estado compartilhado entre as tarefas.
            await Parallel.ForEachAsync(
                mensagens,
                new ParallelOptions { MaxDegreeOfParallelism = 10, CancellationToken = cancellationToken },
                async (mensagem, ct) =>
                {
                    try
                    {
                        var tipo = Type.GetType(mensagem.Tipo);
                        if (tipo is null)
                        {
                            mensagem.Erro = $"Tipo não encontrado: {mensagem.Tipo}";
                            return;
                        }

                        var evento = JsonSerializer.Deserialize(mensagem.Payload, tipo);
                        if (evento is null)
                        {
                            mensagem.Erro = $"Falha ao desserializar payload do tipo {mensagem.Tipo}";
                            return;
                        }

                        await bus.Publish(evento, tipo, ct);
                        mensagem.ProcessadoEm = DateTimeOffset.UtcNow;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Erro ao processar outbox message {Id}", mensagem.Id);
                        mensagem.Erro = ex.Message.Length > 2000
                            ? ex.Message[..2000]
                            : ex.Message;
                    }
                });

            await db.SaveChangesAsync(cancellationToken);
            return mensagens.Count;
        }
        catch (OperationCanceledException)
        {
            return 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro geral no lote do OutboxRelayWorker");
            return 0;
        }
    }
}

using Consolidado.Domain;
using Consolidado.Infrastructure.Persistencia;
using Lancamentos.Domain.Events;
using MassTransit;
using Microsoft.EntityFrameworkCore;

namespace Consolidado.Infrastructure.Consumidores;

public sealed class LancamentoRegistradoConsumidor : IConsumer<LancamentoRegistrado>
{
    private readonly IDbContextFactory<ConsolidadoDbContext> _dbFactory;

    public LancamentoRegistradoConsumidor(IDbContextFactory<ConsolidadoDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    public async Task Consume(ConsumeContext<LancamentoRegistrado> context)
    {
        // MassTransit sempre preenche MessageId, mas o tipo é Guid? por contrato
        if (context.MessageId is not { } messageId)
            return;

        var evento = context.Message;
        DateOnly data = SaldoDiario.CalcularDataLocal(evento.DataOcorrencia);

        var (totalCreditos, totalDebitos, qtdCreditos, qtdDebitos) =
            SaldoDiario.ClassificarLancamento(evento.Tipo, evento.ValorComSinal);

        var id = Guid.CreateVersion7();
        var agora = DateTimeOffset.UtcNow;

        await using var db = await _dbFactory.CreateDbContextAsync(context.CancellationToken);
        await using var tx = await db.Database.BeginTransactionAsync(context.CancellationToken);

        // Tenta registrar o messageId atomicamente. ON CONFLICT DO NOTHING retorna 0 linhas
        // afetadas se o messageId já foi processado — reentrega do RabbitMQ, descartamos.
        var inserida = await db.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO mensagens_processadas ("MessageId", "ProcessadoEm")
            VALUES ({messageId}, {agora})
            ON CONFLICT ("MessageId") DO NOTHING
            """, context.CancellationToken);

        if (inserida == 0)
        {
            await tx.RollbackAsync(context.CancellationToken);
            return;
        }

        await db.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO saldos_diarios ("Id", "ContaId", "Data", "TotalCreditos", "TotalDebitos", "QuantidadeCreditos", "QuantidadeDebitos", "AtualizadoEm")
            VALUES ({id}, {evento.ContaId}, {data}, {totalCreditos}, {totalDebitos}, {qtdCreditos}, {qtdDebitos}, {agora})
            ON CONFLICT ("ContaId", "Data") DO UPDATE
            SET "TotalCreditos" = saldos_diarios."TotalCreditos" + EXCLUDED."TotalCreditos",
                "TotalDebitos" = saldos_diarios."TotalDebitos" + EXCLUDED."TotalDebitos",
                "QuantidadeCreditos" = saldos_diarios."QuantidadeCreditos" + EXCLUDED."QuantidadeCreditos",
                "QuantidadeDebitos" = saldos_diarios."QuantidadeDebitos" + EXCLUDED."QuantidadeDebitos",
                "AtualizadoEm" = EXCLUDED."AtualizadoEm"
            """, context.CancellationToken);

        await tx.CommitAsync(context.CancellationToken);
    }
}

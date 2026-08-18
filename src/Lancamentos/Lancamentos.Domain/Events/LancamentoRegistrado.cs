using FluxoCaixa.Domain.Primitives;

namespace Lancamentos.Domain.Events;

public sealed record LancamentoRegistrado(
    Guid EventId,
    DateTimeOffset OccurredOn,
    Guid LancamentoId,
    Guid ContaId,
    string Tipo,
    decimal ValorAbsoluto,
    decimal ValorComSinal,
    DateTimeOffset DataOcorrencia) : IDomainEvent;

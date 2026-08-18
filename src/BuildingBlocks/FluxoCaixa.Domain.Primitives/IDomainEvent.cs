namespace FluxoCaixa.Domain.Primitives;

public interface IDomainEvent
{
    Guid EventId { get; }
    DateTimeOffset OccurredOn { get; }
}

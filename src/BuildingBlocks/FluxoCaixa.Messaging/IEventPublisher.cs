using FluxoCaixa.Domain.Primitives;

namespace FluxoCaixa.Messaging;

public interface IEventPublisher
{
    Task PublishAsync(IDomainEvent domainEvent, CancellationToken cancellationToken = default);
}

using FluxoCaixa.Domain.Primitives;

namespace FluxoCaixa.Application.Abstractions;

public interface ICommand<TResponse>;

public interface ICommandHandler<TCommand, TResponse>
    where TCommand : ICommand<TResponse>
{
    Task<Result<TResponse>> HandleAsync(TCommand command, CancellationToken cancellationToken = default);
}

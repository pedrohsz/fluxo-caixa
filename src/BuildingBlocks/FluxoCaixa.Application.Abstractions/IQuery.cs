using FluxoCaixa.Domain.Primitives;

namespace FluxoCaixa.Application.Abstractions;

public interface IQuery<TResponse>;

public interface IQueryHandler<TQuery, TResponse>
    where TQuery : IQuery<TResponse>
{
    Task<Result<TResponse>> HandleAsync(TQuery query, CancellationToken cancellationToken = default);
}

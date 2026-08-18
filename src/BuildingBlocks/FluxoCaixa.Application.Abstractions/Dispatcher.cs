using FluxoCaixa.Domain.Primitives;
using Microsoft.Extensions.DependencyInjection;

namespace FluxoCaixa.Application.Abstractions;

public sealed class Dispatcher : IDispatcher
{
    private readonly IServiceProvider _serviceProvider;

    public Dispatcher(IServiceProvider serviceProvider) => _serviceProvider = serviceProvider;

    public Task<Result<TResponse>> SendAsync<TResponse>(
        ICommand<TResponse> command,
        CancellationToken cancellationToken = default)
    {
        var wrapperType = typeof(CommandHandlerWrapper<,>)
            .MakeGenericType(command.GetType(), typeof(TResponse));

        var handlerType = typeof(ICommandHandler<,>)
            .MakeGenericType(command.GetType(), typeof(TResponse));

        var handler = _serviceProvider.GetRequiredService(handlerType);
        var wrapper = (IHandlerWrapper<TResponse>)Activator.CreateInstance(wrapperType, handler)!;

        return wrapper.HandleAsync(command, cancellationToken);
    }

    public Task<Result<TResponse>> QueryAsync<TResponse>(
        IQuery<TResponse> query,
        CancellationToken cancellationToken = default)
    {
        var wrapperType = typeof(QueryHandlerWrapper<,>)
            .MakeGenericType(query.GetType(), typeof(TResponse));

        var handlerType = typeof(IQueryHandler<,>)
            .MakeGenericType(query.GetType(), typeof(TResponse));

        var handler = _serviceProvider.GetRequiredService(handlerType);
        var wrapper = (IHandlerWrapper<TResponse>)Activator.CreateInstance(wrapperType, handler)!;

        return wrapper.HandleAsync(query, cancellationToken);
    }
}

internal interface IHandlerWrapper<TResponse>
{
    Task<Result<TResponse>> HandleAsync(object request, CancellationToken cancellationToken);
}

internal sealed class CommandHandlerWrapper<TCommand, TResponse> : IHandlerWrapper<TResponse>
    where TCommand : ICommand<TResponse>
{
    private readonly ICommandHandler<TCommand, TResponse> _inner;

    public CommandHandlerWrapper(ICommandHandler<TCommand, TResponse> inner) => _inner = inner;

    public Task<Result<TResponse>> HandleAsync(object request, CancellationToken cancellationToken) =>
        _inner.HandleAsync((TCommand)request, cancellationToken);
}

internal sealed class QueryHandlerWrapper<TQuery, TResponse> : IHandlerWrapper<TResponse>
    where TQuery : IQuery<TResponse>
{
    private readonly IQueryHandler<TQuery, TResponse> _inner;

    public QueryHandlerWrapper(IQueryHandler<TQuery, TResponse> inner) => _inner = inner;

    public Task<Result<TResponse>> HandleAsync(object request, CancellationToken cancellationToken) =>
        _inner.HandleAsync((TQuery)request, cancellationToken);
}

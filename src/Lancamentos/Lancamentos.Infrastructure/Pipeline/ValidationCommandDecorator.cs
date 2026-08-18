using FluentValidation;
using FluxoCaixa.Application.Abstractions;
using FluxoCaixa.Domain.Primitives;

namespace Lancamentos.Infrastructure.Pipeline;

public sealed class ValidationCommandDecorator<TCommand, TResponse> : ICommandHandler<TCommand, TResponse>
    where TCommand : ICommand<TResponse>
{
    private readonly ICommandHandler<TCommand, TResponse> _inner;
    private readonly IValidator<TCommand>? _validator;

    public ValidationCommandDecorator(
        ICommandHandler<TCommand, TResponse> inner,
        IValidator<TCommand>? validator = null)
    {
        _inner = inner;
        _validator = validator;
    }

    public async Task<Result<TResponse>> HandleAsync(TCommand command, CancellationToken cancellationToken = default)
    {
        if (_validator is not null)
        {
            var validacao = await _validator.ValidateAsync(command, cancellationToken);
            if (!validacao.IsValid)
            {
                var primeiro = validacao.Errors[0];
                return Error.Validation(primeiro.ErrorCode, primeiro.ErrorMessage);
            }
        }

        return await _inner.HandleAsync(command, cancellationToken);
    }
}

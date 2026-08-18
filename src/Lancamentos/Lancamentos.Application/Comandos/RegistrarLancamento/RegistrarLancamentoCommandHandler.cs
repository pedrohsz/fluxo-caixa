using FluxoCaixa.Application.Abstractions;
using FluxoCaixa.Domain.Primitives;
using Lancamentos.Application.Portas;
using Lancamentos.Domain;

namespace Lancamentos.Application.Comandos.RegistrarLancamento;

public sealed class RegistrarLancamentoCommandHandler
    : ICommandHandler<RegistrarLancamentoCommand, Guid>
{
    private readonly ILancamentoRepositorio _repositorio;

    public RegistrarLancamentoCommandHandler(ILancamentoRepositorio repositorio) =>
        _repositorio = repositorio;

    public async Task<Result<Guid>> HandleAsync(
        RegistrarLancamentoCommand command,
        CancellationToken cancellationToken = default)
    {
        var resultado = Lancamento.Criar(
            command.ContaId,
            command.Tipo,
            command.Valor,
            command.DataOcorrencia,
            command.Descricao,
            command.Categoria);

        if (resultado.IsFailure)
            return resultado.Error;

        await _repositorio.AdicionarAsync(resultado.Value!, cancellationToken);

        return resultado.Value!.Id;
    }
}

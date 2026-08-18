using FluxoCaixa.Application.Abstractions;
using FluxoCaixa.Domain.Primitives;
using Lancamentos.Application.Portas;

namespace Lancamentos.Application.Consultas.ObterLancamento;

public sealed class ObterLancamentoQueryHandler
    : IQueryHandler<ObterLancamentoQuery, LancamentoDto>
{
    private readonly ILancamentoRepositorio _repositorio;

    public ObterLancamentoQueryHandler(ILancamentoRepositorio repositorio) =>
        _repositorio = repositorio;

    public async Task<Result<LancamentoDto>> HandleAsync(
        ObterLancamentoQuery query,
        CancellationToken cancellationToken = default)
    {
        var lancamento = await _repositorio.ObterPorIdAsync(query.Id, cancellationToken);

        if (lancamento is null)
            return LancamentoAppErrors.NaoEncontrado;

        return new LancamentoDto(
            lancamento.Id,
            lancamento.ContaId,
            lancamento.Tipo.Nome,
            lancamento.Valor.Valor,
            lancamento.ValorComSinal,
            lancamento.DataOcorrencia,
            lancamento.Descricao,
            lancamento.Categoria);
    }
}

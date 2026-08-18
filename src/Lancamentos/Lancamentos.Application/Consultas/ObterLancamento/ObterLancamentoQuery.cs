using FluxoCaixa.Application.Abstractions;

namespace Lancamentos.Application.Consultas.ObterLancamento;

public sealed record ObterLancamentoQuery(Guid Id) : IQuery<LancamentoDto>;

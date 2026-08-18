using FluxoCaixa.Application.Abstractions;

namespace Consolidado.Application.Consultas.ObterSaldoDiario;

public sealed record ObterSaldoDiarioQuery(Guid ContaId, DateOnly Data) : IQuery<SaldoDiarioDto>;

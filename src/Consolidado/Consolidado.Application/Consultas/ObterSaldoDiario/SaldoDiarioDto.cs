namespace Consolidado.Application.Consultas.ObterSaldoDiario;

public sealed record SaldoDiarioDto(
    Guid ContaId,
    DateOnly Data,
    decimal TotalCreditos,
    decimal TotalDebitos,
    decimal SaldoLiquido,
    int QuantidadeCreditos,
    int QuantidadeDebitos,
    DateTimeOffset AtualizadoEm);

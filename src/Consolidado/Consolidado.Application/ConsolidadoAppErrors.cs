using FluxoCaixa.Domain.Primitives;

namespace Consolidado.Application;

public static class ConsolidadoAppErrors
{
    public static readonly Error SaldoNaoEncontrado = Error.NotFound(
        "SaldoDiario.NaoEncontrado",
        "Saldo diário não encontrado para a conta e data informadas.");
}

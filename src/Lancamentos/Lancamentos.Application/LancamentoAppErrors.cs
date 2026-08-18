using FluxoCaixa.Domain.Primitives;

namespace Lancamentos.Application;

internal static class LancamentoAppErrors
{
    public static readonly Error NaoEncontrado =
        Error.NotFound("Lancamento.NaoEncontrado", "Lançamento não encontrado.");
}

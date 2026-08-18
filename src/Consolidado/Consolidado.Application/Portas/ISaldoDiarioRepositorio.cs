using Consolidado.Application.Consultas.ObterSaldoDiario;

namespace Consolidado.Application.Portas;

public interface ISaldoDiarioRepositorio
{
    Task<SaldoDiarioDto?> ObterPorContaEDataAsync(
        Guid contaId, DateOnly data, CancellationToken cancellationToken = default);
}

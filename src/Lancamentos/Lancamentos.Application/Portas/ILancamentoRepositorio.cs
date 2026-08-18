using Lancamentos.Domain;

namespace Lancamentos.Application.Portas;

public interface ILancamentoRepositorio
{
    Task AdicionarAsync(Lancamento lancamento, CancellationToken cancellationToken = default);
    Task<Lancamento?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken = default);
}

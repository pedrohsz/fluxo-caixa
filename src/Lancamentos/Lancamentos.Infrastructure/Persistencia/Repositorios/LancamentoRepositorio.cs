using Lancamentos.Application.Portas;
using Lancamentos.Domain;
using Microsoft.EntityFrameworkCore;

namespace Lancamentos.Infrastructure.Persistencia.Repositorios;

public sealed class LancamentoRepositorio : ILancamentoRepositorio
{
    private readonly LancamentosDbContext _dbContext;

    public LancamentoRepositorio(LancamentosDbContext dbContext) => _dbContext = dbContext;

    public async Task AdicionarAsync(Lancamento lancamento, CancellationToken cancellationToken = default)
    {
        await _dbContext.Lancamentos.AddAsync(lancamento, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<Lancamento?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Lancamentos
            .AsNoTracking()
            .FirstOrDefaultAsync(l => l.Id == id, cancellationToken);
    }
}

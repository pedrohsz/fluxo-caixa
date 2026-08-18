using Consolidado.Application.Consultas.ObterSaldoDiario;
using Consolidado.Application.Portas;
using Microsoft.EntityFrameworkCore;

namespace Consolidado.Infrastructure.Persistencia.Repositorios;

public sealed class SaldoDiarioRepositorio : ISaldoDiarioRepositorio
{
    private readonly IDbContextFactory<ConsolidadoDbContext> _dbFactory;

    public SaldoDiarioRepositorio(IDbContextFactory<ConsolidadoDbContext> dbFactory) =>
        _dbFactory = dbFactory;

    public async Task<SaldoDiarioDto?> ObterPorContaEDataAsync(
        Guid contaId, DateOnly data, CancellationToken cancellationToken = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);

        var saldo = await db.SaldosDiarios
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.ContaId == contaId && s.Data == data, cancellationToken);

        if (saldo is null)
            return null;

        return new SaldoDiarioDto(
            saldo.ContaId,
            saldo.Data,
            saldo.TotalCreditos,
            saldo.TotalDebitos,
            saldo.SaldoLiquido,
            saldo.QuantidadeCreditos,
            saldo.QuantidadeDebitos,
            saldo.AtualizadoEm);
    }
}

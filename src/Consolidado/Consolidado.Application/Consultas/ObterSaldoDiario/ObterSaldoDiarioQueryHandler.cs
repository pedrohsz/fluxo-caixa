using System.Text.Json;
using Consolidado.Application.Portas;
using FluxoCaixa.Application.Abstractions;
using FluxoCaixa.Domain.Primitives;
using Microsoft.Extensions.Caching.Distributed;

namespace Consolidado.Application.Consultas.ObterSaldoDiario;

public sealed class ObterSaldoDiarioQueryHandler
    : IQueryHandler<ObterSaldoDiarioQuery, SaldoDiarioDto>
{
    // TTL longo para dados consolidados existentes
    private static readonly DistributedCacheEntryOptions CacheOptions =
        new() { AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(60) };

    // TTL curto para negative cache: evita stampede no Postgres quando o saldo
    // ainda não foi consolidado (404), mas não prende o dado por muito tempo
    private static readonly DistributedCacheEntryOptions MissCacheOptions =
        new() { AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(5) };

    private const string SentinelMiss = "__miss__";

    private readonly ISaldoDiarioRepositorio _repositorio;
    private readonly IDistributedCache _cache;

    public ObterSaldoDiarioQueryHandler(ISaldoDiarioRepositorio repositorio, IDistributedCache cache)
    {
        _repositorio = repositorio;
        _cache = cache;
    }

    public async Task<Result<SaldoDiarioDto>> HandleAsync(
        ObterSaldoDiarioQuery query,
        CancellationToken cancellationToken = default)
    {
        string chave = $"saldo:{query.ContaId}:{query.Data:yyyy-MM-dd}";

        var cached = await _cache.GetStringAsync(chave, cancellationToken);
        if (cached is not null)
        {
            if (cached == SentinelMiss)
                return ConsolidadoAppErrors.SaldoNaoEncontrado;

            return JsonSerializer.Deserialize<SaldoDiarioDto>(cached)!;
        }

        var saldo = await _repositorio.ObterPorContaEDataAsync(
            query.ContaId, query.Data, cancellationToken);

        if (saldo is null)
        {
            await _cache.SetStringAsync(chave, SentinelMiss, MissCacheOptions, cancellationToken);
            return ConsolidadoAppErrors.SaldoNaoEncontrado;
        }

        await _cache.SetStringAsync(
            chave, JsonSerializer.Serialize(saldo), CacheOptions, cancellationToken);

        return saldo;
    }
}

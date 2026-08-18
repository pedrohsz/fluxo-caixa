using System.Text;
using System.Text.Json;
using Consolidado.Application;
using Consolidado.Application.Consultas.ObterSaldoDiario;
using Consolidado.Application.Portas;
using FluentAssertions;
using Microsoft.Extensions.Caching.Distributed;
using NSubstitute;

namespace Consolidado.UnitTests.Consultas;

public sealed class ObterSaldoDiarioQueryHandlerTests
{
    private readonly ISaldoDiarioRepositorio _repositorio = Substitute.For<ISaldoDiarioRepositorio>();
    private readonly IDistributedCache _cache = Substitute.For<IDistributedCache>();
    private readonly ObterSaldoDiarioQueryHandler _sut;

    private static readonly Guid ContaId = Guid.NewGuid();
    private static readonly DateOnly Data = new(2026, 8, 17);
    private static readonly string ChaveCache = $"saldo:{ContaId}:{Data:yyyy-MM-dd}";

    private static readonly SaldoDiarioDto DtoEsperado = new(
        ContaId, Data,
        TotalCreditos: 1000m, TotalDebitos: 300m, SaldoLiquido: 700m,
        QuantidadeCreditos: 3, QuantidadeDebitos: 2,
        AtualizadoEm: DateTimeOffset.UtcNow);

    public ObterSaldoDiarioQueryHandlerTests() =>
        _sut = new ObterSaldoDiarioQueryHandler(_repositorio, _cache);

    [Fact]
    public async Task HandleAsync_CacheMiss_ConsultaRepositorioERetornaDto()
    {
        _cache.GetAsync(ChaveCache, Arg.Any<CancellationToken>())
              .Returns((byte[]?)null);
        _repositorio.ObterPorContaEDataAsync(ContaId, Data, Arg.Any<CancellationToken>())
                    .Returns(DtoEsperado);

        var resultado = await _sut.HandleAsync(new ObterSaldoDiarioQuery(ContaId, Data));

        resultado.IsSuccess.Should().BeTrue();
        resultado.Value!.SaldoLiquido.Should().Be(700m);
        resultado.Value.TotalCreditos.Should().Be(1000m);
        resultado.Value.TotalDebitos.Should().Be(300m);
    }

    [Fact]
    public async Task HandleAsync_CacheMiss_PopulaCacheAposConsultaNoRepositorio()
    {
        _cache.GetAsync(ChaveCache, Arg.Any<CancellationToken>())
              .Returns((byte[]?)null);
        _repositorio.ObterPorContaEDataAsync(ContaId, Data, Arg.Any<CancellationToken>())
                    .Returns(DtoEsperado);

        await _sut.HandleAsync(new ObterSaldoDiarioQuery(ContaId, Data));

        await _cache.Received(1).SetAsync(
            ChaveCache,
            Arg.Any<byte[]>(),
            Arg.Any<DistributedCacheEntryOptions>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_CacheHit_NaoConsultaRepositorio()
    {
        var json = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(DtoEsperado));
        _cache.GetAsync(ChaveCache, Arg.Any<CancellationToken>())
              .Returns(json);

        var resultado = await _sut.HandleAsync(new ObterSaldoDiarioQuery(ContaId, Data));

        resultado.IsSuccess.Should().BeTrue();
        resultado.Value!.SaldoLiquido.Should().Be(DtoEsperado.SaldoLiquido);

        await _repositorio.DidNotReceive()
            .ObterPorContaEDataAsync(Arg.Any<Guid>(), Arg.Any<DateOnly>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_SaldoNaoEncontrado_RetornaErro()
    {
        _cache.GetAsync(ChaveCache, Arg.Any<CancellationToken>())
              .Returns((byte[]?)null);
        _repositorio.ObterPorContaEDataAsync(ContaId, Data, Arg.Any<CancellationToken>())
                    .Returns((SaldoDiarioDto?)null);

        var resultado = await _sut.HandleAsync(new ObterSaldoDiarioQuery(ContaId, Data));

        resultado.IsFailure.Should().BeTrue();
        resultado.Error.Should().Be(ConsolidadoAppErrors.SaldoNaoEncontrado);
    }

    [Fact]
    public async Task HandleAsync_SaldoNaoEncontrado_PopulaCacheSentinelMiss()
    {
        _cache.GetAsync(ChaveCache, Arg.Any<CancellationToken>())
              .Returns((byte[]?)null);
        _repositorio.ObterPorContaEDataAsync(ContaId, Data, Arg.Any<CancellationToken>())
                    .Returns((SaldoDiarioDto?)null);

        await _sut.HandleAsync(new ObterSaldoDiarioQuery(ContaId, Data));

        // Deve cachear o sentinel "__miss__" para evitar stampede no Postgres
        await _cache.Received(1).SetAsync(
            ChaveCache,
            Arg.Is<byte[]>(b => Encoding.UTF8.GetString(b) == "__miss__"),
            Arg.Any<DistributedCacheEntryOptions>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_NegativoCacheHit_RetornaErroSemConsultarRepositorio()
    {
        // Cache retorna o sentinel — simula negative cache hit
        _cache.GetAsync(ChaveCache, Arg.Any<CancellationToken>())
              .Returns(Encoding.UTF8.GetBytes("__miss__"));

        var resultado = await _sut.HandleAsync(new ObterSaldoDiarioQuery(ContaId, Data));

        resultado.IsFailure.Should().BeTrue();
        resultado.Error.Should().Be(ConsolidadoAppErrors.SaldoNaoEncontrado);

        await _repositorio.DidNotReceive()
            .ObterPorContaEDataAsync(Arg.Any<Guid>(), Arg.Any<DateOnly>(), Arg.Any<CancellationToken>());
    }
}

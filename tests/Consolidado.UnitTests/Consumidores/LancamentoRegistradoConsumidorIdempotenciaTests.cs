using Consolidado.Infrastructure.Consumidores;
using Consolidado.Infrastructure.Persistencia;
using FluentAssertions;
using Lancamentos.Domain.Events;
using MassTransit;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using NSubstitute;

namespace Consolidado.UnitTests.Consumidores;

/// <summary>
/// Testa o comportamento de idempotência do consumer usando SQLite in-memory.
/// SQLite suporta ON CONFLICT (upsert) e transações, refletindo o comportamento
/// real do Postgres sem exigir um banco externo.
/// </summary>
public sealed class LancamentoRegistradoConsumidorIdempotenciaTests : IAsyncLifetime
{
    private SqliteConnection _conexao = null!;
    private DbContextOptions<ConsolidadoDbContext> _opts = null!;
    private ConsolidadoDbContext _db = null!;
    private LancamentoRegistradoConsumidor _consumidor = null!;

    public async Task InitializeAsync()
    {
        // Conexão compartilhada: mantém o banco in-memory vivo entre múltiplos DbContexts
        _conexao = new SqliteConnection("Data Source=:memory:");
        await _conexao.OpenAsync();

        _opts = new DbContextOptionsBuilder<ConsolidadoDbContext>()
            .UseSqlite(_conexao)
            .Options;

        _db = new ConsolidadoDbContext(_opts);
        await _db.Database.EnsureCreatedAsync();

        // Factory mockada: cada chamada retorna um novo contexto sobre a mesma conexão
        var factory = Substitute.For<IDbContextFactory<ConsolidadoDbContext>>();
        factory.CreateDbContextAsync(Arg.Any<CancellationToken>())
            .Returns(_ => Task.FromResult(new ConsolidadoDbContext(_opts)));

        _consumidor = new LancamentoRegistradoConsumidor(factory);
    }

    public async Task DisposeAsync()
    {
        await _db.DisposeAsync();
        await _conexao.DisposeAsync();
    }

    // ─── helpers ─────────────────────────────────────────────────────────────

    private static ConsumeContext<LancamentoRegistrado> CriarContexto(
        Guid? messageId, LancamentoRegistrado evento)
    {
        var ctx = Substitute.For<ConsumeContext<LancamentoRegistrado>>();
        ctx.MessageId.Returns(messageId);
        ctx.Message.Returns(evento);
        ctx.CancellationToken.Returns(CancellationToken.None);
        return ctx;
    }

    private static LancamentoRegistrado CriarEvento(
        Guid? contaId = null,
        string tipo = "Credito",
        decimal valor = 100m,
        DateTimeOffset? dataOcorrencia = null)
    {
        var v = tipo == "Credito" ? valor : -valor;
        return new LancamentoRegistrado(
            EventId: Guid.NewGuid(),
            OccurredOn: DateTimeOffset.UtcNow,
            LancamentoId: Guid.NewGuid(),
            ContaId: contaId ?? Guid.NewGuid(),
            Tipo: tipo,
            ValorAbsoluto: valor,
            ValorComSinal: v,
            DataOcorrencia: dataOcorrencia ?? new DateTimeOffset(2026, 8, 15, 12, 0, 0, TimeSpan.Zero));
    }

    // ─── testes ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task Consume_MessageIdNulo_NaoProcessaENaoGravaNada()
    {
        var ctx = CriarContexto(null, CriarEvento());

        await _consumidor.Consume(ctx);

        (await _db.SaldosDiarios.AnyAsync()).Should().BeFalse();
    }

    [Fact]
    public async Task Consume_PrimeiraEntrega_SalvaSaldoCorretamente()
    {
        var ctx = CriarContexto(Guid.NewGuid(), CriarEvento(valor: 150m));

        await _consumidor.Consume(ctx);

        var saldo = await _db.SaldosDiarios.SingleAsync();
        saldo.TotalCreditos.Should().Be(150m);
        saldo.TotalDebitos.Should().Be(0m);
        saldo.QuantidadeCreditos.Should().Be(1);
        saldo.QuantidadeDebitos.Should().Be(0);
    }

    [Fact]
    public async Task Consume_SegundaEntregaMesmoMessageId_NaoReprocessa()
    {
        var messageId = Guid.NewGuid();
        var evento = CriarEvento(valor: 100m);

        // Primeira entrega — normal
        await _consumidor.Consume(CriarContexto(messageId, evento));
        // Segunda entrega — simula reentrega do RabbitMQ (crash entre publish e ack)
        await _consumidor.Consume(CriarContexto(messageId, evento));

        var saldo = await _db.SaldosDiarios.SingleAsync();
        saldo.TotalCreditos.Should().Be(100m, "crédito não pode ser somado duas vezes");
        saldo.QuantidadeCreditos.Should().Be(1, "contagem não pode dobrar na reentrega");
    }

    [Fact]
    public async Task Consume_MensagensDiferentesMesmaContaEData_AcumulaSaldo()
    {
        var contaId = Guid.NewGuid();
        var data = new DateTimeOffset(2026, 8, 15, 12, 0, 0, TimeSpan.Zero);

        await _consumidor.Consume(CriarContexto(Guid.NewGuid(), CriarEvento(contaId, valor: 300m, dataOcorrencia: data)));
        await _consumidor.Consume(CriarContexto(Guid.NewGuid(), CriarEvento(contaId, valor: 200m, dataOcorrencia: data)));

        var saldo = await _db.SaldosDiarios.SingleAsync();
        saldo.TotalCreditos.Should().Be(500m);
        saldo.QuantidadeCreditos.Should().Be(2);
    }

    [Fact]
    public async Task Consume_CreditoEDebito_CalculaSaldoCorretamente()
    {
        var contaId = Guid.NewGuid();
        var data = new DateTimeOffset(2026, 8, 15, 12, 0, 0, TimeSpan.Zero);

        await _consumidor.Consume(CriarContexto(Guid.NewGuid(), CriarEvento(contaId, "Credito", 500m, data)));
        await _consumidor.Consume(CriarContexto(Guid.NewGuid(), CriarEvento(contaId, "Debito",  200m, data)));

        var saldo = await _db.SaldosDiarios.SingleAsync();
        saldo.TotalCreditos.Should().Be(500m);
        saldo.TotalDebitos.Should().Be(200m);
        saldo.SaldoLiquido.Should().Be(300m);
    }

    [Fact]
    public async Task Consume_MultiplasReentregas_SaldoFinalCorretoCom3MensagensDistintas()
    {
        var contaId = Guid.NewGuid();
        var data = new DateTimeOffset(2026, 8, 15, 12, 0, 0, TimeSpan.Zero);

        var msgId1 = Guid.NewGuid();
        var msgId2 = Guid.NewGuid();
        var msgId3 = Guid.NewGuid();
        var ev1 = CriarEvento(contaId, valor: 100m, dataOcorrencia: data);
        var ev2 = CriarEvento(contaId, valor: 50m,  dataOcorrencia: data);
        var ev3 = CriarEvento(contaId, valor: 25m,  dataOcorrencia: data);

        await _consumidor.Consume(CriarContexto(msgId1, ev1)); // processa
        await _consumidor.Consume(CriarContexto(msgId2, ev2)); // processa
        await _consumidor.Consume(CriarContexto(msgId1, ev1)); // reentrega — descarta
        await _consumidor.Consume(CriarContexto(msgId3, ev3)); // processa
        await _consumidor.Consume(CriarContexto(msgId2, ev2)); // reentrega — descarta
        await _consumidor.Consume(CriarContexto(msgId1, ev1)); // reentrega — descarta

        var saldo = await _db.SaldosDiarios.SingleAsync();
        saldo.TotalCreditos.Should().Be(175m, "apenas 3 mensagens distintas devem ser somadas");
        saldo.QuantidadeCreditos.Should().Be(3);
    }
}

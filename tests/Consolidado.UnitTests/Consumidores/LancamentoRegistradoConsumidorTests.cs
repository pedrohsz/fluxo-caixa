using Consolidado.Domain;
using FluentAssertions;

namespace Consolidado.UnitTests.Consumidores;

public sealed class LancamentoRegistradoConsumidorTests
{
    // -------------------------------------------------------------------------
    // Testes de ClassificarLancamento
    // -------------------------------------------------------------------------

    [Fact]
    public void ClassificarLancamento_Credito_DevePreencher_TotalCreditosEQuantidadeCreditos()
    {
        // Arrange
        const string tipo = "Credito";
        const decimal valorComSinal = 150.50m;

        // Act
        var (totalCreditos, totalDebitos, qtdCreditos, qtdDebitos) =
            SaldoDiario.ClassificarLancamento(tipo, valorComSinal);

        // Assert
        totalCreditos.Should().Be(150.50m);
        totalDebitos.Should().Be(0m);
        qtdCreditos.Should().Be(1);
        qtdDebitos.Should().Be(0);
    }

    [Fact]
    public void ClassificarLancamento_Debito_DevePreencher_TotalDebitosEQuantidadeDebitos()
    {
        // Arrange
        const string tipo = "Debito";
        const decimal valorComSinal = -200.00m; // valor com sinal negativo

        // Act
        var (totalCreditos, totalDebitos, qtdCreditos, qtdDebitos) =
            SaldoDiario.ClassificarLancamento(tipo, valorComSinal);

        // Assert
        totalCreditos.Should().Be(0m);
        totalDebitos.Should().Be(200.00m); // valor absoluto
        qtdCreditos.Should().Be(0);
        qtdDebitos.Should().Be(1);
    }

    // -------------------------------------------------------------------------
    // Testes de CalcularDataLocal — fuso America/Sao_Paulo (UTC-3 no inverno)
    // -------------------------------------------------------------------------

    [Fact]
    public void CalcularDataLocal_DataUtcMeiaNoite_DeveRetornarDiaAnteriorNoBrasil()
    {
        // UTC 2026-01-15 00:00:00 → Brasília = 2026-01-14 21:00:00 (UTC-3 no inverno)
        var dataUtc = new DateTimeOffset(2026, 1, 15, 0, 0, 0, TimeSpan.Zero);

        DateOnly dataLocal = SaldoDiario.CalcularDataLocal(dataUtc);

        dataLocal.Should().Be(new DateOnly(2026, 1, 14));
    }

    [Fact]
    public void CalcularDataLocal_DataUtcTresHoras_DeveRetornarMesmoDiaNoBrasil()
    {
        // UTC 2026-01-15 03:00:00 → Brasília = 2026-01-15 00:00:00 (UTC-3 no inverno)
        var dataUtc = new DateTimeOffset(2026, 1, 15, 3, 0, 0, TimeSpan.Zero);

        DateOnly dataLocal = SaldoDiario.CalcularDataLocal(dataUtc);

        dataLocal.Should().Be(new DateOnly(2026, 1, 15));
    }

    [Fact]
    public void CalcularDataLocal_HorarioVerao_DeveUsarOffsetCorreto()
    {
        // Brazil usou horário de verão historicamente (UTC-2).
        // Em novembro de 2019 (último ano com DST no Brasil), Brasília ficava UTC-2.
        // UTC 2019-11-04 01:30:00 → Brasília = 2019-11-03 23:30:00 (UTC-2 no verão)
        var dataUtc = new DateTimeOffset(2019, 11, 4, 1, 30, 0, TimeSpan.Zero);

        DateOnly dataLocal = SaldoDiario.CalcularDataLocal(dataUtc);

        // Em UTC-2 às 01:30 UTC → 23:30 do dia anterior
        dataLocal.Should().Be(new DateOnly(2019, 11, 3));
    }

    // -------------------------------------------------------------------------
    // Teste de SaldoLiquido
    // -------------------------------------------------------------------------

    [Fact]
    public void SaldoLiquido_DeveSerDiferencaEntreCreditosEDebitos()
    {
        // SaldoDiario recém-criado tem TotalCreditos = 0 e TotalDebitos = 0
        var saldo = SaldoDiario.Criar(Guid.NewGuid(), new DateOnly(2026, 8, 17));

        saldo.SaldoLiquido.Should().Be(saldo.TotalCreditos - saldo.TotalDebitos);
        saldo.SaldoLiquido.Should().Be(0m);
    }

    // -------------------------------------------------------------------------
    // Teste adicional: Criar inicializa zerado
    // -------------------------------------------------------------------------

    [Fact]
    public void Criar_DeveInicializarValoresZerados()
    {
        var contaId = Guid.NewGuid();
        var data = new DateOnly(2026, 8, 17);

        var saldo = SaldoDiario.Criar(contaId, data);

        saldo.ContaId.Should().Be(contaId);
        saldo.Data.Should().Be(data);
        saldo.TotalCreditos.Should().Be(0m);
        saldo.TotalDebitos.Should().Be(0m);
        saldo.QuantidadeCreditos.Should().Be(0);
        saldo.QuantidadeDebitos.Should().Be(0);
        saldo.SaldoLiquido.Should().Be(0m);
    }
}

using FluentAssertions;
using Lancamentos.Domain;

namespace Lancamentos.UnitTests.Domain;

public sealed class DinheiroTests
{
    [Theory]
    [InlineData(0.01)]
    [InlineData(1)]
    [InlineData(999999.99)]
    public void Criar_ComValorPositivo_RetornaSuccesso(decimal valor)
    {
        var result = Dinheiro.Criar(valor);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Valor.Should().Be(valor);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-0.01)]
    [InlineData(-100)]
    public void Criar_ComValorNaoPositivo_RetornaFalha(decimal valor)
    {
        var result = Dinheiro.Criar(valor);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(LancamentoErrors.ValorDeveSerPositivo);
    }

    [Fact]
    public void AplicarSinal_Credito_RetornaPositivo()
    {
        var dinheiro = Dinheiro.Criar(100m).Value!;
        dinheiro.AplicarSinal(TipoLancamento.Credito).Should().Be(100m);
    }

    [Fact]
    public void AplicarSinal_Debito_RetornaNegativo()
    {
        var dinheiro = Dinheiro.Criar(100m).Value!;
        dinheiro.AplicarSinal(TipoLancamento.Debito).Should().Be(-100m);
    }

    [Fact]
    public void Igualdade_MesmoValor_SaoIguais()
    {
        var a = Dinheiro.Criar(50m).Value!;
        var b = Dinheiro.Criar(50m).Value!;

        a.Should().Be(b);
        (a == b).Should().BeTrue();
    }

    [Fact]
    public void Igualdade_ValoresDiferentes_NaoSaoIguais()
    {
        var a = Dinheiro.Criar(50m).Value!;
        var b = Dinheiro.Criar(51m).Value!;

        a.Should().NotBe(b);
        (a != b).Should().BeTrue();
    }
}

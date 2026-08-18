using FluentAssertions;
using Lancamentos.Domain;

namespace Lancamentos.UnitTests.Domain;

public sealed class TipoLancamentoTests
{
    [Theory]
    [InlineData("Credito")]
    [InlineData("credito")]
    [InlineData("CREDITO")]
    public void TryParse_Credito_CaseInsensitive_RetornaCredito(string input)
    {
        var parsed = TipoLancamento.TryParse(input, out var tipo);

        parsed.Should().BeTrue();
        tipo.Should().Be(TipoLancamento.Credito);
    }

    [Theory]
    [InlineData("Debito")]
    [InlineData("debito")]
    [InlineData("DEBITO")]
    public void TryParse_Debito_CaseInsensitive_RetornaDebito(string input)
    {
        var parsed = TipoLancamento.TryParse(input, out var tipo);

        parsed.Should().BeTrue();
        tipo.Should().Be(TipoLancamento.Debito);
    }

    [Fact]
    public void TryParse_TipoInexistente_RetornaFalse()
    {
        var parsed = TipoLancamento.TryParse("Invalido", out var tipo);

        parsed.Should().BeFalse();
        tipo.Should().BeNull();
    }

    [Fact]
    public void Credito_Sinal_EhPositivo()
    {
        TipoLancamento.Credito.Sinal.Should().Be(1);
    }

    [Fact]
    public void Debito_Sinal_EhNegativo()
    {
        TipoLancamento.Debito.Sinal.Should().Be(-1);
    }

    [Fact]
    public void Igualdade_MesmoTipo_SaoIguais()
    {
        var a = TipoLancamento.Credito;
        var b = TipoLancamento.Credito;
        a.Should().Be(b);
        (a == b).Should().BeTrue();
    }

    [Fact]
    public void Igualdade_TiposDiferentes_NaoSaoIguais()
    {
        (TipoLancamento.Credito != TipoLancamento.Debito).Should().BeTrue();
    }

    [Fact]
    public void Parse_TipoInexistente_LancaExcecao()
    {
        var act = () => TipoLancamento.Parse("Invalido");
        act.Should().Throw<ArgumentOutOfRangeException>();
    }
}

using FluentAssertions;
using Lancamentos.Application.Comandos.RegistrarLancamento;

namespace Lancamentos.UnitTests.Application;

public sealed class RegistrarLancamentoValidatorTests
{
    private readonly RegistrarLancamentoValidator _sut = new();

    private static readonly Guid ContaId = Guid.NewGuid();
    private static readonly DateTimeOffset DataPassada = DateTimeOffset.UtcNow.AddDays(-1);

    private RegistrarLancamentoCommand ComandoValido(
        Guid? contaId = null,
        string tipo = "Credito",
        decimal valor = 100m,
        DateTimeOffset? data = null,
        string descricao = "Venda") =>
        new(contaId ?? ContaId, tipo, valor, data ?? DataPassada, descricao, null);

    [Fact]
    public async Task Validar_CommandoValido_SemErros()
    {
        var resultado = await _sut.ValidateAsync(ComandoValido());

        resultado.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Validar_ContaIdVazio_RetornaErro()
    {
        var resultado = await _sut.ValidateAsync(ComandoValido(contaId: Guid.Empty));

        resultado.IsValid.Should().BeFalse();
        resultado.Errors.Should().Contain(e => e.ErrorCode == "Lancamento.ContaIdInvalido");
    }

    [Theory]
    [InlineData("Invalido")]
    [InlineData("")]
    public async Task Validar_TipoInvalido_RetornaErro(string tipo)
    {
        var resultado = await _sut.ValidateAsync(ComandoValido(tipo: tipo));

        resultado.IsValid.Should().BeFalse();
        resultado.Errors.Should().Contain(e => e.ErrorCode == "Lancamento.TipoInvalido");
    }

    [Theory]
    [InlineData("Credito")]
    [InlineData("Debito")]
    public async Task Validar_TiposValidos_SemErros(string tipo)
    {
        var resultado = await _sut.ValidateAsync(ComandoValido(tipo: tipo));

        resultado.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task Validar_ValorNaoPositivo_RetornaErro(decimal valor)
    {
        var resultado = await _sut.ValidateAsync(ComandoValido(valor: valor));

        resultado.IsValid.Should().BeFalse();
        resultado.Errors.Should().Contain(e => e.ErrorCode == "Lancamento.ValorInvalido");
    }

    [Fact]
    public async Task Validar_DataFutura_RetornaErro()
    {
        var resultado = await _sut.ValidateAsync(ComandoValido(data: DateTimeOffset.UtcNow.AddSeconds(60)));

        resultado.IsValid.Should().BeFalse();
        resultado.Errors.Should().Contain(e => e.ErrorCode == "Lancamento.DataFutura");
    }

    [Fact]
    public async Task Validar_DescricaoVazia_RetornaErro()
    {
        var resultado = await _sut.ValidateAsync(ComandoValido(descricao: ""));

        resultado.IsValid.Should().BeFalse();
        resultado.Errors.Should().Contain(e => e.ErrorCode == "Lancamento.DescricaoObrigatoria");
    }

    [Fact]
    public async Task Validar_DescricaoComMaisDe200Chars_RetornaErro()
    {
        var resultado = await _sut.ValidateAsync(ComandoValido(descricao: new string('x', 201)));

        resultado.IsValid.Should().BeFalse();
        resultado.Errors.Should().Contain(e => e.ErrorCode == "Lancamento.DescricaoMuitoLonga");
    }
}

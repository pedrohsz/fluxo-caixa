using FluentAssertions;
using Lancamentos.Application.Consultas.ObterLancamento;
using Lancamentos.Application.Portas;
using Lancamentos.Domain;
using NSubstitute;

namespace Lancamentos.UnitTests.Application;

public sealed class ObterLancamentoQueryHandlerTests
{
    private readonly ILancamentoRepositorio _repositorio = Substitute.For<ILancamentoRepositorio>();
    private readonly ObterLancamentoQueryHandler _sut;

    private static readonly DateTimeOffset DataPassada = DateTimeOffset.UtcNow.AddDays(-1);

    public ObterLancamentoQueryHandlerTests() =>
        _sut = new ObterLancamentoQueryHandler(_repositorio);

    [Fact]
    public async Task HandleAsync_LancamentoExiste_RetornaDtoCorreto()
    {
        var lancamento = Lancamento.Criar(Guid.NewGuid(), "Credito", 300m, DataPassada, "Receita mensal", "Receitas").Value!;
        _repositorio.ObterPorIdAsync(lancamento.Id, Arg.Any<CancellationToken>()).Returns(lancamento);

        var resultado = await _sut.HandleAsync(new ObterLancamentoQuery(lancamento.Id));

        resultado.IsSuccess.Should().BeTrue();
        resultado.Value!.Id.Should().Be(lancamento.Id);
        resultado.Value.Tipo.Should().Be("Credito");
        resultado.Value.ValorAbsoluto.Should().Be(300m);
        resultado.Value.ValorComSinal.Should().Be(300m);
        resultado.Value.Descricao.Should().Be("Receita mensal");
        resultado.Value.Categoria.Should().Be("Receitas");
    }

    [Fact]
    public async Task HandleAsync_LancamentoDebito_ValorComSinalEhNegativo()
    {
        var lancamento = Lancamento.Criar(Guid.NewGuid(), "Debito", 80m, DataPassada, "Custo operacional").Value!;
        _repositorio.ObterPorIdAsync(lancamento.Id, Arg.Any<CancellationToken>()).Returns(lancamento);

        var resultado = await _sut.HandleAsync(new ObterLancamentoQuery(lancamento.Id));

        resultado.IsSuccess.Should().BeTrue();
        resultado.Value!.ValorComSinal.Should().Be(-80m);
    }

    [Fact]
    public async Task HandleAsync_LancamentoNaoExiste_RetornaFalhaNaoEncontrado()
    {
        var idInexistente = Guid.NewGuid();
        _repositorio.ObterPorIdAsync(idInexistente, Arg.Any<CancellationToken>()).Returns((Lancamento?)null);

        var resultado = await _sut.HandleAsync(new ObterLancamentoQuery(idInexistente));

        resultado.IsFailure.Should().BeTrue();
        resultado.Error.Code.Should().Be("Lancamento.NaoEncontrado");
    }
}

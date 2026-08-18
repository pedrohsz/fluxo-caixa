using FluentAssertions;
using Lancamentos.Application.Comandos.RegistrarLancamento;
using Lancamentos.Application.Portas;
using Lancamentos.Domain;
using NSubstitute;

namespace Lancamentos.UnitTests.Application;

public sealed class RegistrarLancamentoCommandHandlerTests
{
    private readonly ILancamentoRepositorio _repositorio = Substitute.For<ILancamentoRepositorio>();
    private readonly RegistrarLancamentoCommandHandler _sut;

    private static readonly Guid ContaId = Guid.NewGuid();
    private static readonly DateTimeOffset DataPassada = DateTimeOffset.UtcNow.AddDays(-1);

    public RegistrarLancamentoCommandHandlerTests() =>
        _sut = new RegistrarLancamentoCommandHandler(_repositorio);

    [Fact]
    public async Task HandleAsync_CommandoValido_RetornaIdDoLancamento()
    {
        var command = new RegistrarLancamentoCommand(ContaId, "Credito", 150m, DataPassada, "Venda", null);

        var resultado = await _sut.HandleAsync(command);

        resultado.IsSuccess.Should().BeTrue();
        resultado.Value.Should().NotBeEmpty();
    }

    [Fact]
    public async Task HandleAsync_CommandoValido_PersisteLancamentoNoRepositorio()
    {
        var command = new RegistrarLancamentoCommand(ContaId, "Debito", 50m, DataPassada, "Pagamento", "Fornecedores");

        await _sut.HandleAsync(command);

        await _repositorio.Received(1).AdicionarAsync(
            Arg.Is<Lancamento>(l =>
                l.ContaId == ContaId &&
                l.Tipo == TipoLancamento.Debito &&
                l.Valor.Valor == 50m),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_CommandoValido_LancamentoPeristidoTemDadosCorretos()
    {
        Lancamento? lancamentoSalvo = null;
        await _repositorio.AdicionarAsync(
            Arg.Do<Lancamento>(l => lancamentoSalvo = l),
            Arg.Any<CancellationToken>());

        var command = new RegistrarLancamentoCommand(ContaId, "Credito", 200m, DataPassada, "Serviço prestado", "Serviços");

        var resultado = await _sut.HandleAsync(command);

        lancamentoSalvo.Should().NotBeNull();
        lancamentoSalvo!.Id.Should().Be(resultado.Value);
        lancamentoSalvo.Descricao.Should().Be("Serviço prestado");
        lancamentoSalvo.Categoria.Should().Be("Serviços");
        lancamentoSalvo.ValorComSinal.Should().Be(200m);
    }

    [Fact]
    public async Task HandleAsync_ContaIdVazio_NaoPersisteERetornaFalha()
    {
        var command = new RegistrarLancamentoCommand(Guid.Empty, "Credito", 100m, DataPassada, "Desc", null);

        var resultado = await _sut.HandleAsync(command);

        resultado.IsFailure.Should().BeTrue();
        await _repositorio.DidNotReceive().AdicionarAsync(Arg.Any<Lancamento>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_ValorNegativo_NaoPersisteERetornaFalha()
    {
        var command = new RegistrarLancamentoCommand(ContaId, "Credito", -1m, DataPassada, "Desc", null);

        var resultado = await _sut.HandleAsync(command);

        resultado.IsFailure.Should().BeTrue();
        await _repositorio.DidNotReceive().AdicionarAsync(Arg.Any<Lancamento>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_DataFutura_NaoPersisteERetornaFalha()
    {
        var dataFutura = DateTimeOffset.UtcNow.AddHours(1);
        var command = new RegistrarLancamentoCommand(ContaId, "Credito", 100m, dataFutura, "Desc", null);

        var resultado = await _sut.HandleAsync(command);

        resultado.IsFailure.Should().BeTrue();
        await _repositorio.DidNotReceive().AdicionarAsync(Arg.Any<Lancamento>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_CancellationToken_EhPropagadoAoRepositorio()
    {
        var cts = new CancellationTokenSource();
        var command = new RegistrarLancamentoCommand(ContaId, "Credito", 100m, DataPassada, "Desc", null);

        await _sut.HandleAsync(command, cts.Token);

        await _repositorio.Received(1).AdicionarAsync(Arg.Any<Lancamento>(), cts.Token);
    }
}

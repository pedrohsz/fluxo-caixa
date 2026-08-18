using FluentAssertions;
using Lancamentos.Domain;
using Lancamentos.Domain.Events;

namespace Lancamentos.UnitTests.Domain;

public sealed class LancamentoCriarTests
{
    private static readonly Guid ContaId = Guid.NewGuid();
    private static readonly DateTimeOffset Agora = DateTimeOffset.UtcNow;
    private static readonly DateTimeOffset DataPassada = Agora.AddDays(-1);

    // --- SUCESSO ---

    [Fact]
    public void Criar_ComDadosValidos_Credito_RetornaSuccesso()
    {
        var result = Lancamento.Criar(ContaId, "Credito", 100m, DataPassada, "Venda de produto", agora: Agora);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Tipo.Should().Be(TipoLancamento.Credito);
        result.Value.ValorComSinal.Should().Be(100m);
        result.Value.Descricao.Should().Be("Venda de produto");
    }

    [Fact]
    public void Criar_ComDadosValidos_Debito_RetornaSuccesso()
    {
        var result = Lancamento.Criar(ContaId, "Debito", 50m, DataPassada, "Pagamento fornecedor", agora: Agora);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Tipo.Should().Be(TipoLancamento.Debito);
        result.Value.ValorComSinal.Should().Be(-50m);
    }

    [Fact]
    public void Criar_ComCategoria_PreservaCategoria()
    {
        var result = Lancamento.Criar(ContaId, "Credito", 200m, DataPassada, "Desc", "Vendas", agora: Agora);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Categoria.Should().Be("Vendas");
    }

    [Fact]
    public void Criar_SemCategoria_CategoriaEhNull()
    {
        var result = Lancamento.Criar(ContaId, "Credito", 200m, DataPassada, "Desc", agora: Agora);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Categoria.Should().BeNull();
    }

    [Fact]
    public void Criar_ComDescricaoComEspacos_TrimaDescricao()
    {
        var result = Lancamento.Criar(ContaId, "Credito", 100m, DataPassada, "  Desc com espaços  ", agora: Agora);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Descricao.Should().Be("Desc com espaços");
    }

    [Fact]
    public void Criar_ComDataExatamenteAgora_RetornaSuccesso()
    {
        var result = Lancamento.Criar(ContaId, "Credito", 1m, Agora, "Ok", agora: Agora);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void Criar_RaiseLancamentoRegistradoEvent()
    {
        var result = Lancamento.Criar(ContaId, "Credito", 100m, DataPassada, "Venda", agora: Agora);

        result.IsSuccess.Should().BeTrue();
        result.Value!.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<LancamentoRegistrado>();
    }

    [Fact]
    public void Criar_EventoLancamentoRegistrado_TemDadosCorretos()
    {
        var result = Lancamento.Criar(ContaId, "Debito", 75m, DataPassada, "Compra", agora: Agora);

        var @event = result.Value!.DomainEvents.OfType<LancamentoRegistrado>().Single();
        @event.LancamentoId.Should().Be(result.Value.Id);
        @event.ContaId.Should().Be(ContaId);
        @event.Tipo.Should().Be("Debito");
        @event.ValorAbsoluto.Should().Be(75m);
        @event.ValorComSinal.Should().Be(-75m);
    }

    [Fact]
    public void Criar_IdGerado_NaoEhEmpty()
    {
        var result = Lancamento.Criar(ContaId, "Credito", 1m, DataPassada, "Desc", agora: Agora);

        result.Value!.Id.Should().NotBeEmpty();
    }

    // --- ERROS DE DOMÍNIO ---

    [Fact]
    public void Criar_ContaIdVazio_RetornaFalha()
    {
        var result = Lancamento.Criar(Guid.Empty, "Credito", 100m, DataPassada, "Desc", agora: Agora);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(LancamentoErrors.ContaIdInvalido);
    }

    [Theory]
    [InlineData("TipoInexistente")]
    [InlineData("")]
    [InlineData("  ")]
    public void Criar_TipoInvalido_RetornaFalha(string tipo)
    {
        var result = Lancamento.Criar(ContaId, tipo, 100m, DataPassada, "Desc", agora: Agora);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(LancamentoErrors.TipoInvalido);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-0.01)]
    [InlineData(-100)]
    public void Criar_ValorNaoPositivo_RetornaFalha(decimal valor)
    {
        var result = Lancamento.Criar(ContaId, "Credito", valor, DataPassada, "Desc", agora: Agora);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(LancamentoErrors.ValorDeveSerPositivo);
    }

    [Fact]
    public void Criar_DataFutura_RetornaFalha()
    {
        var dataFutura = Agora.AddSeconds(1);
        var result = Lancamento.Criar(ContaId, "Credito", 100m, dataFutura, "Desc", agora: Agora);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(LancamentoErrors.DataNaoPodeSerfutura);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Criar_DescricaoVaziaOuNula_RetornaFalha(string? descricao)
    {
        var result = Lancamento.Criar(ContaId, "Credito", 100m, DataPassada, descricao!, agora: Agora);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(LancamentoErrors.DescricaoObrigatoria);
    }

    [Fact]
    public void Criar_DescricaoComMaisDe200Chars_RetornaFalha()
    {
        var descricaoLonga = new string('x', 201);
        var result = Lancamento.Criar(ContaId, "Credito", 100m, DataPassada, descricaoLonga, agora: Agora);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(LancamentoErrors.DescricaoMuitoLonga);
    }

    [Fact]
    public void Criar_DescricaoComExatamente200Chars_RetornaSuccesso()
    {
        var descricao200 = new string('x', 200);
        var result = Lancamento.Criar(ContaId, "Credito", 100m, DataPassada, descricao200, agora: Agora);

        result.IsSuccess.Should().BeTrue();
    }
}

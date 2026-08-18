using FluxoCaixa.Domain.Primitives;
using Lancamentos.Domain.Events;

namespace Lancamentos.Domain;

public sealed class Lancamento : Entity
{
    // Construtor para EF Core reconstituir do banco sem passar pelas invariantes.
    private Lancamento() : base(Guid.Empty)
    {
        Tipo = null!;
        Valor = null!;
        Descricao = null!;
    }

    private Lancamento(
        Guid id,
        Guid contaId,
        TipoLancamento tipo,
        Dinheiro valor,
        DateTimeOffset dataOcorrencia,
        string descricao,
        string? categoria)
        : base(id)
    {
        ContaId = contaId;
        Tipo = tipo;
        Valor = valor;
        DataOcorrencia = dataOcorrencia;
        Descricao = descricao;
        Categoria = categoria;
    }

    public Guid ContaId { get; private set; }
    public TipoLancamento Tipo { get; private set; }
    public Dinheiro Valor { get; private set; }
    public DateTimeOffset DataOcorrencia { get; private set; }
    public string Descricao { get; private set; }
    public string? Categoria { get; private set; }

    public decimal ValorComSinal => Valor.AplicarSinal(Tipo);

    public static Result<Lancamento> Criar(
        Guid contaId,
        string tipo,
        decimal valor,
        DateTimeOffset dataOcorrencia,
        string descricao,
        string? categoria = null,
        DateTimeOffset? agora = null)
    {
        var now = agora ?? DateTimeOffset.UtcNow;

        if (contaId == Guid.Empty)
            return LancamentoErrors.ContaIdInvalido;

        if (!TipoLancamento.TryParse(tipo, out var tipoLancamento) || tipoLancamento is null)
            return LancamentoErrors.TipoInvalido;

        var dinheiroResult = Dinheiro.Criar(valor);
        if (dinheiroResult.IsFailure)
            return dinheiroResult.Error;

        if (dataOcorrencia > now)
            return LancamentoErrors.DataNaoPodeSerfutura;

        if (string.IsNullOrWhiteSpace(descricao))
            return LancamentoErrors.DescricaoObrigatoria;

        if (descricao.Length > 200)
            return LancamentoErrors.DescricaoMuitoLonga;

        var lancamento = new Lancamento(
            Guid.CreateVersion7(),
            contaId,
            tipoLancamento,
            dinheiroResult.Value!,
            dataOcorrencia,
            descricao.Trim(),
            categoria?.Trim());

        lancamento.RaiseDomainEvent(new LancamentoRegistrado(
            EventId: Guid.CreateVersion7(),
            OccurredOn: now,
            LancamentoId: lancamento.Id,
            ContaId: lancamento.ContaId,
            Tipo: tipoLancamento.Nome,
            ValorAbsoluto: dinheiroResult.Value!.Valor,
            ValorComSinal: lancamento.ValorComSinal,
            DataOcorrencia: dataOcorrencia));

        return lancamento;
    }
}

using FluxoCaixa.Domain.Primitives;

namespace Lancamentos.Domain;

public static class LancamentoErrors
{
    public static readonly Error ValorDeveSerPositivo =
        Error.Validation("Lancamento.ValorInvalido", "O valor do lançamento deve ser maior que zero.");

    public static readonly Error DataNaoPodeSerfutura =
        Error.Validation("Lancamento.DataFutura", "A data de ocorrência não pode ser futura.");

    public static readonly Error DescricaoObrigatoria =
        Error.Validation("Lancamento.DescricaoObrigatoria", "A descrição é obrigatória.");

    public static readonly Error DescricaoMuitoLonga =
        Error.Validation("Lancamento.DescricaoMuitoLonga", "A descrição não pode exceder 200 caracteres.");

    public static readonly Error ContaIdInvalido =
        Error.Validation("Lancamento.ContaIdInvalido", "O identificador da conta é inválido.");

    public static readonly Error TipoInvalido =
        Error.Validation("Lancamento.TipoInvalido", "O tipo de lançamento é inválido.");
}

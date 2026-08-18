using FluxoCaixa.Domain.Primitives;

namespace Lancamentos.Domain;

public sealed class Dinheiro : ValueObject
{
    private Dinheiro(decimal valor) => Valor = valor;

    public decimal Valor { get; }

    public static Result<Dinheiro> Criar(decimal valor)
    {
        if (valor <= 0)
            return LancamentoErrors.ValorDeveSerPositivo;

        return new Dinheiro(valor);
    }

    public decimal AplicarSinal(TipoLancamento tipo) => Valor * tipo.Sinal;

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Valor;
    }

    public override string ToString() => Valor.ToString("C2", System.Globalization.CultureInfo.GetCultureInfo("pt-BR"));
}

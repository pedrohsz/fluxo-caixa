namespace Consolidado.Domain;

public sealed class SaldoDiario
{
    private static readonly TimeZoneInfo _tz = TimeZoneInfo.FindSystemTimeZoneById("America/Sao_Paulo");

#pragma warning disable CS8618 // construtor privado para EF Core
    private SaldoDiario() { }
#pragma warning restore CS8618

    public Guid Id { get; private set; }
    public Guid ContaId { get; private set; }
    public DateOnly Data { get; private set; }
    public decimal TotalCreditos { get; private set; }
    public decimal TotalDebitos { get; private set; }
    public int QuantidadeCreditos { get; private set; }
    public int QuantidadeDebitos { get; private set; }
    public DateTimeOffset AtualizadoEm { get; private set; }

    public decimal SaldoLiquido => TotalCreditos - TotalDebitos;

    public static SaldoDiario Criar(Guid contaId, DateOnly data) =>
        new()
        {
            Id = Guid.CreateVersion7(),
            ContaId = contaId,
            Data = data,
            TotalCreditos = 0m,
            TotalDebitos = 0m,
            QuantidadeCreditos = 0,
            QuantidadeDebitos = 0,
            AtualizadoEm = DateTimeOffset.UtcNow
        };

    /// <summary>
    /// Converte um instante UTC para a data local no fuso horário de Brasília.
    /// Método público estático para facilitar testes unitários.
    /// </summary>
    public static DateOnly CalcularDataLocal(DateTimeOffset dataUtc) =>
        DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(dataUtc, _tz).DateTime);

    /// <summary>
    /// Calcula os valores de crédito/débito a partir de um lançamento.
    /// Retorna (totalCreditos, totalDebitos, qtdCreditos, qtdDebitos).
    /// Método público estático para facilitar testes unitários.
    /// </summary>
    public static (decimal TotalCreditos, decimal TotalDebitos, int QtdCreditos, int QtdDebitos)
        ClassificarLancamento(string tipo, decimal valorComSinal)
    {
        bool ehCredito = string.Equals(tipo, "Credito", StringComparison.OrdinalIgnoreCase);
        decimal valorAbs = Math.Abs(valorComSinal);
        return ehCredito
            ? (valorAbs, 0m, 1, 0)
            : (0m, valorAbs, 0, 1);
    }
}

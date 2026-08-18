namespace Lancamentos.Domain;

public sealed class TipoLancamento : IEquatable<TipoLancamento>
{
    public static readonly TipoLancamento Credito = new("Credito", 1);
    public static readonly TipoLancamento Debito = new("Debito", -1);

    private static readonly Dictionary<string, TipoLancamento> _all = new(StringComparer.OrdinalIgnoreCase)
    {
        [Credito.Nome] = Credito,
        [Debito.Nome] = Debito,
    };

    private TipoLancamento(string nome, int sinal)
    {
        Nome = nome;
        Sinal = sinal;
    }

    public string Nome { get; }

    /// <summary>Multiplica o valor absoluto para obter o impacto no saldo (+1 crédito, -1 débito).</summary>
    public int Sinal { get; }

    public static bool TryParse(string nome, out TipoLancamento? tipo) =>
        _all.TryGetValue(nome, out tipo);

    public static TipoLancamento Parse(string nome) =>
        _all.TryGetValue(nome, out var tipo)
            ? tipo
            : throw new ArgumentOutOfRangeException(nameof(nome), $"Tipo de lançamento inválido: '{nome}'.");

    public bool Equals(TipoLancamento? other) => other is not null && Nome == other.Nome;
    public override bool Equals(object? obj) => Equals(obj as TipoLancamento);
    public override int GetHashCode() => Nome.GetHashCode(StringComparison.Ordinal);
    public override string ToString() => Nome;

    public static bool operator ==(TipoLancamento? left, TipoLancamento? right) =>
        left?.Equals(right) ?? right is null;

    public static bool operator !=(TipoLancamento? left, TipoLancamento? right) => !(left == right);
}

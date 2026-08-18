namespace FluxoCaixa.Domain.Primitives;

public sealed record Error(string Code, string Description)
{
    public static readonly Error None = new(string.Empty, string.Empty);

    public static Error Validation(string code, string description) => new(code, description);
    public static Error NotFound(string code, string description) => new(code, description);
}

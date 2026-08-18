namespace Lancamentos.Api.Contratos;

public sealed record RegistrarLancamentoRequest(
    Guid ContaId,
    string Tipo,
    decimal Valor,
    DateTimeOffset DataOcorrencia,
    string Descricao,
    string? Categoria);

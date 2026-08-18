namespace Lancamentos.Application.Consultas.ObterLancamento;

public sealed record LancamentoDto(
    Guid Id,
    Guid ContaId,
    string Tipo,
    decimal ValorAbsoluto,
    decimal ValorComSinal,
    DateTimeOffset DataOcorrencia,
    string Descricao,
    string? Categoria);

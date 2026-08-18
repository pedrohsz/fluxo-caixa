using FluxoCaixa.Application.Abstractions;

namespace Lancamentos.Application.Comandos.RegistrarLancamento;

public sealed record RegistrarLancamentoCommand(
    Guid ContaId,
    string Tipo,
    decimal Valor,
    DateTimeOffset DataOcorrencia,
    string Descricao,
    string? Categoria) : ICommand<Guid>;

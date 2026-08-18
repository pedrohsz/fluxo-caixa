using FluentValidation;

namespace Lancamentos.Application.Comandos.RegistrarLancamento;

public sealed class RegistrarLancamentoValidator : AbstractValidator<RegistrarLancamentoCommand>
{
    public RegistrarLancamentoValidator()
    {
        RuleFor(c => c.ContaId)
            .NotEmpty()
            .WithErrorCode("Lancamento.ContaIdInvalido")
            .WithMessage("O identificador da conta é inválido.");

        RuleFor(c => c.Tipo)
            .NotEmpty()
            .Must(t => t == "Credito" || t == "Debito")
            .WithErrorCode("Lancamento.TipoInvalido")
            .WithMessage("O tipo deve ser 'Credito' ou 'Debito'.");

        RuleFor(c => c.Valor)
            .GreaterThan(0)
            .WithErrorCode("Lancamento.ValorInvalido")
            .WithMessage("O valor do lançamento deve ser maior que zero.");

        RuleFor(c => c.DataOcorrencia)
            .LessThanOrEqualTo(_ => DateTimeOffset.UtcNow)
            .WithErrorCode("Lancamento.DataFutura")
            .WithMessage("A data de ocorrência não pode ser futura.");

        RuleFor(c => c.Descricao)
            .NotEmpty()
            .WithErrorCode("Lancamento.DescricaoObrigatoria")
            .WithMessage("A descrição é obrigatória.")
            .MaximumLength(200)
            .WithErrorCode("Lancamento.DescricaoMuitoLonga")
            .WithMessage("A descrição não pode exceder 200 caracteres.");
    }
}

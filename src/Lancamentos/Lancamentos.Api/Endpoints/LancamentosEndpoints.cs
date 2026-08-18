using FluxoCaixa.Application.Abstractions;
using Lancamentos.Api.Contratos;
using Lancamentos.Api.Extensions;
using Lancamentos.Application.Comandos.RegistrarLancamento;
using Lancamentos.Application.Consultas.ObterLancamento;
using Microsoft.AspNetCore.Mvc;

namespace Lancamentos.Api.Endpoints;

public static class LancamentosEndpoints
{
    public static IEndpointRouteBuilder MapLancamentos(this IEndpointRouteBuilder app)
    {
        var grupo = app.MapGroup("/api/lancamentos")
            .WithTags("Lançamentos");

        grupo.MapPost("/", RegistrarLancamento)
            .WithName("RegistrarLancamento")
            .WithSummary("Registra um novo lançamento financeiro")
            .Produces<RegistrarLancamentoResponse>(StatusCodes.Status201Created)
            .ProducesValidationProblem(StatusCodes.Status422UnprocessableEntity)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        grupo.MapGet("/{id:guid}", ObterLancamento)
            .WithName("ObterLancamento")
            .WithSummary("Retorna um lançamento pelo ID")
            .Produces<LancamentoDto>()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        return app;
    }

    private static async Task<IResult> RegistrarLancamento(
        [FromBody] RegistrarLancamentoRequest request,
        IDispatcher dispatcher,
        CancellationToken cancellationToken)
    {
        var command = new RegistrarLancamentoCommand(
            request.ContaId,
            request.Tipo,
            request.Valor,
            request.DataOcorrencia,
            request.Descricao,
            request.Categoria);

        var resultado = await dispatcher.SendAsync(command, cancellationToken);

        return resultado.IsSuccess
            ? Results.CreatedAtRoute("ObterLancamento", new { id = resultado.Value }, new RegistrarLancamentoResponse(resultado.Value))
            : resultado.Error.ParaHttpResult();
    }

    private static async Task<IResult> ObterLancamento(
        Guid id,
        IDispatcher dispatcher,
        CancellationToken cancellationToken)
    {
        var resultado = await dispatcher.QueryAsync(new ObterLancamentoQuery(id), cancellationToken);

        return resultado.IsSuccess
            ? Results.Ok(resultado.Value)
            : resultado.Error.ParaHttpResult();
    }

    private sealed record RegistrarLancamentoResponse(Guid Id);
}

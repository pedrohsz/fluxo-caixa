using Consolidado.Api.Extensions;
using Consolidado.Application.Consultas.ObterSaldoDiario;
using FluxoCaixa.Application.Abstractions;

namespace Consolidado.Api.Endpoints;

public static class ConsolidadoEndpoints
{
    public static IEndpointRouteBuilder MapConsolidado(this IEndpointRouteBuilder app)
    {
        var grupo = app.MapGroup("/api/consolidado")
            .WithTags("Consolidado");

        grupo.MapGet("/{contaId:guid}/{data}", ObterSaldoDiario)
            .WithName("ObterSaldoDiario")
            .WithSummary("Retorna o saldo diário consolidado de uma conta para uma data")
            .Produces<SaldoDiarioDto>()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        return app;
    }

    private static async Task<IResult> ObterSaldoDiario(
        Guid contaId,
        DateOnly data,
        IDispatcher dispatcher,
        CancellationToken cancellationToken)
    {
        var resultado = await dispatcher.QueryAsync(
            new ObterSaldoDiarioQuery(contaId, data), cancellationToken);

        return resultado.IsSuccess
            ? Results.Ok(resultado.Value)
            : resultado.Error.ParaHttpResult();
    }
}

using FluxoCaixa.Domain.Primitives;
using Microsoft.AspNetCore.Http;

namespace Lancamentos.Api.Extensions;

internal static class ErrorExtensions
{
    internal static IResult ParaHttpResult(this Error error)
    {
        var statusCode = error.Code.EndsWith("NaoEncontrado", StringComparison.Ordinal)
            ? StatusCodes.Status404NotFound
            : StatusCodes.Status422UnprocessableEntity;

        return Results.Problem(
            detail: error.Description,
            statusCode: statusCode,
            title: statusCode == 404 ? "Recurso não encontrado" : "Requisição inválida",
            extensions: new Dictionary<string, object?> { ["codigo"] = error.Code });
    }
}

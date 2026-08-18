using System.Text.Json;
using Lancamentos.Api.Endpoints;
using Lancamentos.Api.HealthChecks;
using Lancamentos.Infrastructure;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddHealthChecks()
    .AddCheck<PostgresLancamentosHealthCheck>("postgres", tags: ["ready"]);
// MassTransit registra automaticamente o check do RabbitMQ com tag "ready"

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(opts => opts.WithTitle("Lançamentos API"));
}

await app.Services.AplicarMigracoesAsync();

app.UseExceptionHandler();
app.UseStatusCodePages();

app.MapLancamentos();

app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false });
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready"),
    ResponseWriter = EscreverRespostaJson
});

app.Run();

static Task EscreverRespostaJson(HttpContext ctx, HealthReport report)
{
    ctx.Response.ContentType = "application/json";
    return ctx.Response.WriteAsync(JsonSerializer.Serialize(new
    {
        status = report.Status.ToString(),
        checks = report.Entries.Select(e => new
        {
            name = e.Key,
            status = e.Value.Status.ToString(),
            description = e.Value.Description,
            duration = e.Value.Duration.ToString()
        }),
        totalDuration = report.TotalDuration.ToString()
    }));
}

// Necessário para testes de integração referenciarem o assembly
public partial class Program { }

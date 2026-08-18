using System.Text.Json;
using Consolidado.Api.Endpoints;
using Consolidado.Api.HealthChecks;
using Consolidado.Infrastructure;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();
builder.Services.AddConsolidadoInfrastructure(builder.Configuration);

builder.Services.AddHealthChecks()
    .AddCheck<PostgresConsolidadoHealthCheck>("postgres", tags: ["ready"])
    .AddCheck<RedisHealthCheck>("redis", tags: ["ready"]);
// MassTransit registra automaticamente o check do RabbitMQ com tag "ready"

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(opts => opts.WithTitle("Consolidado API"));
}

app.UseExceptionHandler();
app.UseStatusCodePages();

app.MapConsolidado();

app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false });
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready"),
    ResponseWriter = EscreverRespostaJson
});

await app.Services.AplicarMigracoesConsolidadoAsync();

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

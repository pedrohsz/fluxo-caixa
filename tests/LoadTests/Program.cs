using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using NBomber.Contracts;
using NBomber.Contracts.Stats;
using NBomber.CSharp;

var config = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: true)
    .AddEnvironmentVariables()
    .Build();

var lancamentosUrl = config["LancamentosUrl"] ?? "http://127.0.0.1:5278";
var consolidadoUrl = config["ConsolidadoUrl"] ?? "http://127.0.0.1:5279";

if (args.Contains("--stress"))
{
    await StressTest.RunAsync(lancamentosUrl, consolidadoUrl);
    return;
}

using var lancamentosClient = new HttpClient { BaseAddress = new Uri(lancamentosUrl) };
using var consolidadoClient = new HttpClient { BaseAddress = new Uri(consolidadoUrl) };

// ContaId fixo para que os lançamentos acumulem saldo durante o teste
var contaId = Guid.NewGuid();

// A consolidação agrupa por data LOCAL de America/Sao_Paulo, não por data UTC.
// Usar a data UTC aqui faria todo GET retornar 404 entre 00:00 e 03:00 UTC.
var hoje = DataLocalConsolidacao.Hoje().ToString("yyyy-MM-dd");

// Aquece: registra um lançamento inicial para garantir que o saldo exista
try
{
    await lancamentosClient.PostAsync("/api/lancamentos",
        new StringContent(JsonSerializer.Serialize(new
        {
            ContaId = contaId,
            Tipo = "Credito",
            Valor = 1000.00m,
            DataOcorrencia = DateTimeOffset.UtcNow,
            Descricao = "Seed inicial de carga"
        }), Encoding.UTF8, "application/json"));

    // Aguarda propagação via RabbitMQ → Consolidado
    await Task.Delay(TimeSpan.FromSeconds(3));
}
catch
{
    Console.WriteLine("[aviso] Aquecimento falhou — serviços podem estar offline.");
}

// ── Cenário 1: registrar lançamento (caminho de escrita) ──────────────────
var registrarLancamento = Scenario.Create("registrar_lancamento", async ctx =>
{
    var tipo = ctx.InvocationNumber % 2 == 0 ? "Credito" : "Debito";
    var payload = new StringContent(
        JsonSerializer.Serialize(new
        {
            ContaId = contaId,
            Tipo = tipo,
            Valor = 50.00m,
            DataOcorrencia = DateTimeOffset.UtcNow,
            Descricao = $"Carga #{ctx.InvocationNumber}"
        }),
        Encoding.UTF8,
        "application/json");

    var response = await lancamentosClient.PostAsync("/api/lancamentos", payload);

    return response.IsSuccessStatusCode
        ? Response.Ok(statusCode: ((int)response.StatusCode).ToString())
        : Response.Fail(statusCode: ((int)response.StatusCode).ToString());
})
.WithoutWarmUp()
.WithLoadSimulations(
    Simulation.Inject(rate: 50, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromSeconds(60))
);

// ── Cenário 2: leitura de saldo consolidado (cache Redis) ─────────────────
var obterSaldo = Scenario.Create("obter_saldo_consolidado", async ctx =>
{
    var response = await consolidadoClient.GetAsync(
        $"/api/consolidado/{contaId}/{hoje}");

    // 200 = cache hit; 404 = dado ainda não consolidado (aceitável no início)
    return response.StatusCode is System.Net.HttpStatusCode.OK
                                 or System.Net.HttpStatusCode.NotFound
        ? Response.Ok(statusCode: ((int)response.StatusCode).ToString())
        : Response.Fail(statusCode: ((int)response.StatusCode).ToString());
})
.WithoutWarmUp()
.WithLoadSimulations(
    Simulation.Inject(rate: 200, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromSeconds(60))
);

// ── Runner ────────────────────────────────────────────────────────────────
NBomberRunner
    .RegisterScenarios(registrarLancamento, obterSaldo)
    .WithReportFolder("reports")
    .WithReportFormats(ReportFormat.Html, ReportFormat.Csv, ReportFormat.Md)
    .Run();

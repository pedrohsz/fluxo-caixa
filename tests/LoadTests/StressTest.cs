using System.Text;
using System.Text.Json;
using NBomber.Contracts.Stats;
using NBomber.CSharp;

/// <summary>
/// Stress test com ramp-up em degraus para encontrar o ponto de ruptura.
/// Executar com: dotnet run --project tests/LoadTests -- --stress
/// </summary>
public static class StressTest
{
    public static async Task RunAsync(string lancamentosUrl, string consolidadoUrl)
    {
        using var postClient = new HttpClient { BaseAddress = new Uri(lancamentosUrl) };
        using var getClient  = new HttpClient { BaseAddress = new Uri(consolidadoUrl) };

        var contaId = Guid.NewGuid();

        // A consolidação agrupa por data LOCAL de America/Sao_Paulo, não por data UTC.
        // Usar a data UTC aqui faria todo GET retornar 404 entre 00:00 e 03:00 UTC.
        var hoje    = DataLocalConsolidacao.Hoje().ToString("yyyy-MM-dd");

        Console.WriteLine("[stress] Semeando dados e aguardando consolidação...");
        await SeedAsync(postClient, contaId, quantidade: 20);
        await Task.Delay(TimeSpan.FromSeconds(12)); // OutboxRelay (2 s) + consumer + margem
        Console.WriteLine("[stress] Pronto. Iniciando ramp-up...");

        // ── Cenário A: escrita (POST /api/lancamentos) ────────────────────────
        // Degraus: 50 → 100 → 200 → 300 → 500 → 750 req/s
        var postStress = Scenario.Create("stress_registrar_lancamento", async ctx =>
        {
            var tipo = ctx.InvocationNumber % 2 == 0 ? "Credito" : "Debito";
            var content = new StringContent(
                JsonSerializer.Serialize(new
                {
                    ContaId = contaId,
                    Tipo = tipo,
                    Valor = 10.00m,
                    DataOcorrencia = DateTimeOffset.UtcNow,
                    Descricao = $"Stress #{ctx.InvocationNumber}"
                }),
                Encoding.UTF8, "application/json");

            var resp = await postClient.PostAsync("/api/lancamentos", content);

            return resp.IsSuccessStatusCode
                ? Response.Ok(statusCode: ((int)resp.StatusCode).ToString())
                : Response.Fail(statusCode: ((int)resp.StatusCode).ToString());
        })
        .WithoutWarmUp()
        .WithLoadSimulations(
            Simulation.Inject(rate:   50, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromSeconds(20)),
            Simulation.Inject(rate:  100, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromSeconds(20)),
            Simulation.Inject(rate:  200, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromSeconds(20)),
            Simulation.Inject(rate:  400, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromSeconds(20)),
            Simulation.Inject(rate:  600, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromSeconds(20)),
            Simulation.Inject(rate:  800, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromSeconds(20)),
            Simulation.Inject(rate: 1000, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromSeconds(20)),
            Simulation.Inject(rate: 1500, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromSeconds(20)),
            Simulation.Inject(rate: 2000, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromSeconds(20))
        );

        // ── Cenário B: leitura com cache (GET /api/consolidado) ───────────────
        // Degraus: 200 → 500 → 1000 → 2000 → 3000 req/s
        var getStress = Scenario.Create("stress_obter_saldo_consolidado", async ctx =>
        {
            var resp = await getClient.GetAsync($"/api/consolidado/{contaId}/{hoje}");

            // 200 (cache hit) e 404 (ainda sem dado) são ambos válidos
            return resp.StatusCode is System.Net.HttpStatusCode.OK
                                     or System.Net.HttpStatusCode.NotFound
                ? Response.Ok(statusCode: ((int)resp.StatusCode).ToString())
                : Response.Fail(statusCode: ((int)resp.StatusCode).ToString());
        })
        .WithoutWarmUp()
        .WithLoadSimulations(
            Simulation.Inject(rate:   500, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromSeconds(20)),
            Simulation.Inject(rate:  1000, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromSeconds(20)),
            Simulation.Inject(rate:  2000, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromSeconds(20)),
            Simulation.Inject(rate:  4000, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromSeconds(20)),
            Simulation.Inject(rate:  6000, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromSeconds(20)),
            Simulation.Inject(rate:  8000, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromSeconds(20)),
            Simulation.Inject(rate: 10000, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromSeconds(20))
        );

        NBomberRunner
            .RegisterScenarios(postStress, getStress)
            .WithTestName("stress_test")
            .WithReportFolder("reports/stress")
            .WithReportFormats(ReportFormat.Html, ReportFormat.Md)
            .Run();
    }

    private static async Task SeedAsync(HttpClient client, Guid contaId, int quantidade)
    {
        // Semeia sequencialmente para não sobrecarregar o port-forward do Docker no arranque
        for (var i = 0; i < quantidade; i++)
        {
            var payload = new StringContent(
                JsonSerializer.Serialize(new
                {
                    ContaId = contaId,
                    Tipo = i % 2 == 0 ? "Credito" : "Debito",
                    Valor = 100.00m,
                    DataOcorrencia = DateTimeOffset.UtcNow,
                    Descricao = $"Seed stress #{i}"
                }),
                Encoding.UTF8, "application/json");

            for (var tentativa = 1; ; tentativa++)
            {
                try
                {
                    await client.PostAsync("/api/lancamentos", payload);
                    break;
                }
                catch when (tentativa < 4)
                {
                    await Task.Delay(200 * tentativa);
                    payload = new StringContent(payload.ReadAsStringAsync().Result,
                        Encoding.UTF8, "application/json");
                }
            }
        }
    }
}

/// <summary>
/// Calcula a data de consolidação da mesma forma que o serviço Consolidado
/// (<c>SaldoDiario.CalcularDataLocal</c>): convertendo o instante UTC para o
/// fuso de America/Sao_Paulo antes de extrair a data.
///
/// Sem isso, os testes montariam a URL com a data UTC enquanto o consumidor
/// grava o saldo sob a data local, e todo GET retornaria 404 na janela entre
/// 00:00 e 03:00 UTC (21:00 às 24:00 em Brasília).
/// </summary>
public static class DataLocalConsolidacao
{
    private static readonly TimeZoneInfo _tz = TimeZoneInfo.FindSystemTimeZoneById("America/Sao_Paulo");

    public static DateOnly Hoje() => De(DateTimeOffset.UtcNow);

    public static DateOnly De(DateTimeOffset instanteUtc) =>
        DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(instanteUtc, _tz).DateTime);
}

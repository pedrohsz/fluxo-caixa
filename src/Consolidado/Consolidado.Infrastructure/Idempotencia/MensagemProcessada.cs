namespace Consolidado.Infrastructure.Idempotencia;

internal sealed class MensagemProcessada
{
    public Guid MessageId { get; set; }
    public DateTimeOffset ProcessadoEm { get; set; }
}

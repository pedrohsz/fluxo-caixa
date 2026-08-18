namespace Lancamentos.Infrastructure.Outbox;

public sealed class OutboxMessage
{
    public Guid Id { get; set; }
    public string Tipo { get; set; } = string.Empty;
    public string Payload { get; set; } = string.Empty;
    public DateTimeOffset CriadoEm { get; set; }
    public DateTimeOffset? ProcessadoEm { get; set; }
    public string? Erro { get; set; }
}

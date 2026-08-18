using Consolidado.Infrastructure.Idempotencia;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Consolidado.Infrastructure.Persistencia.Configuracoes;

internal sealed class MensagemProcessadaConfiguracao : IEntityTypeConfiguration<MensagemProcessada>
{
    public void Configure(EntityTypeBuilder<MensagemProcessada> builder)
    {
        builder.ToTable("mensagens_processadas");
        builder.HasKey(m => m.MessageId);
        builder.Property(m => m.MessageId).ValueGeneratedNever();
        builder.Property(m => m.ProcessadoEm).IsRequired();
    }
}

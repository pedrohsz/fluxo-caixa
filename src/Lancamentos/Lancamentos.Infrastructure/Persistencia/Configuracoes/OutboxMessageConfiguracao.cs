using Lancamentos.Infrastructure.Outbox;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lancamentos.Infrastructure.Persistencia.Configuracoes;

public sealed class OutboxMessageConfiguracao : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.ToTable("outbox_messages");

        builder.HasKey(o => o.Id);

        builder.Property(o => o.Id).ValueGeneratedNever();
        builder.Property(o => o.Tipo).HasMaxLength(500).IsRequired();
        builder.Property(o => o.Payload).HasColumnType("jsonb").IsRequired();
        builder.Property(o => o.CriadoEm).IsRequired();
        builder.Property(o => o.ProcessadoEm);
        builder.Property(o => o.Erro).HasMaxLength(2000);

        builder.HasIndex(o => o.ProcessadoEm);
    }
}

using Consolidado.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Consolidado.Infrastructure.Persistencia.Configuracoes;

public sealed class SaldoDiarioConfiguracao : IEntityTypeConfiguration<SaldoDiario>
{
    public void Configure(EntityTypeBuilder<SaldoDiario> builder)
    {
        builder.ToTable("saldos_diarios");

        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).ValueGeneratedNever();

        builder.Property(s => s.ContaId).IsRequired();

        builder.Property(s => s.Data)
            .HasColumnType("date")
            .IsRequired();

        builder.Property(s => s.TotalCreditos)
            .HasColumnType("numeric(18,2)")
            .IsRequired();

        builder.Property(s => s.TotalDebitos)
            .HasColumnType("numeric(18,2)")
            .IsRequired();

        builder.Property(s => s.QuantidadeCreditos).IsRequired();
        builder.Property(s => s.QuantidadeDebitos).IsRequired();
        builder.Property(s => s.AtualizadoEm).IsRequired();

        builder.Ignore(s => s.SaldoLiquido);

        builder.HasIndex(s => new { s.ContaId, s.Data }).IsUnique();
    }
}

using Lancamentos.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lancamentos.Infrastructure.Persistencia.Configuracoes;

public sealed class LancamentoConfiguracao : IEntityTypeConfiguration<Lancamento>
{
    public void Configure(EntityTypeBuilder<Lancamento> builder)
    {
        builder.ToTable("lancamentos");

        builder.HasKey(l => l.Id);

        builder.Property(l => l.Id)
            .ValueGeneratedNever();

        builder.Property(l => l.ContaId)
            .IsRequired();

        builder.Property(l => l.Tipo)
            .HasConversion(
                t => t.Nome,
                n => TipoLancamento.Parse(n))
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(l => l.Valor)
            .HasConversion(
                d => d.Valor,
                v => Dinheiro.Criar(v).Value!)
            .HasColumnName("valor")
            .HasColumnType("numeric(18,2)")
            .IsRequired();

        builder.Property(l => l.DataOcorrencia)
            .IsRequired();

        builder.Property(l => l.Descricao)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(l => l.Categoria)
            .HasMaxLength(100);

        builder.Ignore(l => l.ValorComSinal);
        builder.Ignore(l => l.DomainEvents);

        builder.HasIndex(l => l.ContaId);
    }
}

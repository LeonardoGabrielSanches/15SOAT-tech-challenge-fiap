using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Oficina.Domain.Atendimento.Entities;
using Oficina.Domain.Atendimento.Entities.ValueObjects;

namespace Oficina.Infrastructure.Persistencia.Atendimento;

public sealed class VeiculoConfiguration : IEntityTypeConfiguration<Veiculo>
{
    public void Configure(EntityTypeBuilder<Veiculo> builder)
    {
        builder.ToTable("veiculos");
        builder.HasKey(v => v.Id);
        builder.Property(v => v.Id).ValueGeneratedNever();
        builder.Property(v => v.Placa)
            .HasConversion(p => p.Valor, v => Placa.Criar(v))
            .HasMaxLength(7)
            .IsRequired();
        builder.HasIndex(v => v.Placa).IsUnique();
        builder.Property(v => v.Marca).HasMaxLength(100).IsRequired();
        builder.Property(v => v.Modelo).HasMaxLength(100).IsRequired();
        builder.Property(v => v.AnoFabricacao)
            .HasConversion(a => a.Valor, v => AnoFabricacaoPersistido(v))
            .IsRequired();
        builder.Property(v => v.ProprietarioId).IsRequired();
        builder.HasIndex(v => v.ProprietarioId);
        builder.Property(v => v.Ativo).IsRequired();
        builder.HasOne<Cliente>().WithMany().HasForeignKey(v => v.ProprietarioId).OnDelete(DeleteBehavior.Restrict);

        builder.Ignore(v => v.EventosDeDominio);
    }

    private static AnoFabricacao AnoFabricacaoPersistido(int valor) =>
        AnoFabricacao.Criar(valor, TimeProvider.System);
}

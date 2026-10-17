using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Oficina.Domain.Common;
using Oficina.Domain.Estoque;

namespace Oficina.Infrastructure.Persistencia.Configuracoes;

public sealed class PecaInsumoConfiguracao : IEntityTypeConfiguration<PecaInsumo>
{
    public void Configure(EntityTypeBuilder<PecaInsumo> builder)
    {
        builder.ToTable("pecas_insumos");
        builder.HasKey(p => p.Id);
        builder.Ignore(p => p.EventosDeDominio);
        builder.Property(p => p.Id).ValueGeneratedNever();
        builder.Property(p => p.Nome).HasMaxLength(200).IsRequired();
        builder.Property(p => p.Tipo).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(p => p.UnidadeDeMedida).HasMaxLength(30).IsRequired();
        builder.Property(p => p.Preco)
            .HasConversion(d => d.Valor, v => new Dinheiro(v))
            .HasColumnType("numeric(12,2)");
        builder.Property(p => p.Saldo).HasColumnType("numeric(14,3)");
        builder.Property(p => p.Ativo);
        builder.Property<uint>("xmin")
            .HasColumnType("xid")
            .ValueGeneratedOnAddOrUpdate()
            .IsConcurrencyToken();
    }
}

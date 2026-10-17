using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Oficina.Domain.Atendimento.Entities;
using Oficina.Domain.Atendimento.Entities.ValueObjects;

namespace Oficina.Infrastructure.Persistencia.Atendimento;

public sealed class ClienteConfiguration : IEntityTypeConfiguration<Cliente>
{
    public void Configure(EntityTypeBuilder<Cliente> builder)
    {
        builder.ToTable("clientes");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).ValueGeneratedNever();
        builder.Property(c => c.Nome).HasMaxLength(200).IsRequired();
        builder.Property(c => c.Telefone).HasMaxLength(20);
        builder.Property(c => c.Ativo).IsRequired();
        builder.Property(c => c.Email)
            .HasConversion(e => e.Endereco, v => Email.Criar(v))
            .HasMaxLength(254)
            .IsRequired();

        builder.OwnsOne(c => c.Documento, d =>
        {
            d.Property(x => x.Tipo).HasColumnName("documento_tipo").HasConversion<string>().HasMaxLength(10).IsRequired();
            d.Property(x => x.Numero).HasColumnName("documento_numero").HasMaxLength(14).IsRequired();
            d.HasIndex(x => x.Numero).IsUnique();
        });
        builder.Navigation(c => c.Documento).IsRequired();

        builder.Ignore(c => c.EventosDeDominio);
    }
}

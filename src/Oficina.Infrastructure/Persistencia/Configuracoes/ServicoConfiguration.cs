using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Oficina.Domain.CatalogoEstoque.Entities;

namespace Oficina.Infrastructure.Persistencia.Configuracoes;

public sealed class ServicoConfiguration : IEntityTypeConfiguration<Servico>
{
    public void Configure(EntityTypeBuilder<Servico> builder)
    {
        builder.ToTable("servicos");
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).ValueGeneratedNever();
        builder.Property(s => s.Nome).HasMaxLength(200).IsRequired();
        builder.Property(s => s.Descricao).HasMaxLength(1000);
        builder.Property(s => s.Ativo).IsRequired();
        builder.ComplexProperty(s => s.Preco, p => p.Property(d => d.Valor).HasColumnName("preco").HasPrecision(12, 2));
        builder.Ignore(s => s.EventosDeDominio);
    }
}

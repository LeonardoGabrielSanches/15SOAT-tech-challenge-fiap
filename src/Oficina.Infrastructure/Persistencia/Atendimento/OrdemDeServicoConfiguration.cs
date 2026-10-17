using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Oficina.Domain.Atendimento.Entities;
using Oficina.Domain.Atendimento.Entities.Enums;
using Oficina.Domain.Atendimento.Entities.ValueObjects;
using Oficina.Domain.CatalogoEstoque.Entities;

namespace Oficina.Infrastructure.Persistencia.Atendimento;

public sealed class OrdemDeServicoConfiguration : IEntityTypeConfiguration<OrdemDeServico>
{
    public void Configure(EntityTypeBuilder<OrdemDeServico> builder)
    {
        builder.ToTable("ordens_de_servico");
        builder.HasKey(o => o.Id);
        builder.Property(o => o.Id).ValueGeneratedNever();
        builder.Property(o => o.Numero)
            .HasConversion(n => n.Valor, v => NumeroOs.Reconstituir(v))
            .HasMaxLength(13)
            .IsRequired();
        builder.HasIndex(o => o.Numero).IsUnique();
        builder.Property(o => o.ClienteId).IsRequired();
        builder.Property(o => o.VeiculoId).IsRequired();
        builder.Property(o => o.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(o => o.AbertaEm).IsRequired();
        builder.HasIndex(o => o.ClienteId);
        builder.HasIndex(o => o.VeiculoId)
            .IsUnique()
            .HasDatabaseName("ux_ordens_de_servico_veiculo_em_andamento")
            .HasFilter($"status NOT IN ('{nameof(StatusOs.Entregue)}', '{nameof(StatusOs.Cancelada)}')");
        builder.HasOne<Cliente>().WithMany().HasForeignKey(o => o.ClienteId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Veiculo>().WithMany().HasForeignKey(o => o.VeiculoId).OnDelete(DeleteBehavior.Restrict);

        builder.OwnsMany(o => o.Servicos, servicos =>
        {
            servicos.ToTable("ordens_de_servico_servicos");
            servicos.WithOwner().HasForeignKey("OrdemDeServicoId");
            servicos.HasKey("OrdemDeServicoId", nameof(ServicoDaOs.ServicoId));
            servicos.Property(s => s.ServicoId).ValueGeneratedNever();
            servicos.HasOne<Servico>().WithMany().HasForeignKey(s => s.ServicoId).OnDelete(DeleteBehavior.Restrict);
        });
        builder.Navigation(o => o.Servicos).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.OwnsMany(o => o.Historico, historico =>
        {
            historico.ToTable("ordens_de_servico_historico");
            historico.WithOwner().HasForeignKey("OrdemDeServicoId");
            historico.Property<int>("Id").ValueGeneratedOnAdd();
            historico.HasKey("Id");
            historico.Property(h => h.Origem).HasConversion<string>().HasMaxLength(30);
            historico.Property(h => h.Destino).HasConversion<string>().HasMaxLength(30).IsRequired();
            historico.Property(h => h.Momento).IsRequired();
            historico.Property(h => h.Autor).HasMaxLength(200).IsRequired();
        });
        builder.Navigation(o => o.Historico).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.Ignore(o => o.EventosDeDominio);
    }
}

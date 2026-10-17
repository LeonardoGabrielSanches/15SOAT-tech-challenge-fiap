using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Oficina.Domain.Atendimento.Entities;
using Oficina.Domain.Atendimento.Entities.Enums;
using Oficina.Domain.Atendimento.Entities.ValueObjects;
using Oficina.Domain.CatalogoEstoque.Entities;
using Oficina.Domain.Estoque;

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

        builder.Property(o => o.MotivoDaUltimaRecusa).HasMaxLength(500);

        builder.OwnsMany(o => o.ItensDePecaInsumo, itens =>
        {
            itens.ToTable("ordens_de_servico_itens");
            itens.WithOwner().HasForeignKey("OrdemDeServicoId");
            itens.HasKey("OrdemDeServicoId", nameof(ItemDaOs.PecaInsumoId));
            itens.Property(i => i.PecaInsumoId).ValueGeneratedNever();
            itens.Property(i => i.Quantidade).HasColumnType("numeric(14,3)").IsRequired();
            itens.HasOne<PecaInsumo>().WithMany().HasForeignKey(i => i.PecaInsumoId).OnDelete(DeleteBehavior.Restrict);
        });
        builder.Navigation(o => o.ItensDePecaInsumo).UsePropertyAccessMode(PropertyAccessMode.Field);

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

        builder.OwnsMany(o => o.Versoes, versoes =>
        {
            versoes.ToTable("ordens_de_servico_versoes");
            versoes.WithOwner().HasForeignKey("OrdemDeServicoId");
            versoes.HasKey("OrdemDeServicoId", nameof(VersaoDoOrcamento.Numero));
            versoes.Property(v => v.Numero).ValueGeneratedNever();
            versoes.Property(v => v.GeradaEm).IsRequired();
            versoes.Property(v => v.Total).HasColumnType("numeric(14,2)").IsRequired();
            versoes.Property(v => v.Situacao).HasConversion<string>().HasMaxLength(20).IsRequired();
            versoes.Property(v => v.Token).HasMaxLength(64).IsRequired();
            versoes.HasIndex(v => v.Token).IsUnique();
            versoes.Property(v => v.MotivoDaRecusa).HasMaxLength(500);
            versoes.OwnsMany(v => v.Itens, itens =>
            {
                itens.ToTable("ordens_de_servico_versoes_itens");
                itens.WithOwner().HasForeignKey("OrdemDeServicoId", "VersaoNumero");
                itens.Property(i => i.Id).ValueGeneratedOnAdd();
                itens.HasKey(nameof(ItemDaVersao.Id));
                itens.Property(i => i.Tipo).HasConversion<string>().HasMaxLength(20).IsRequired();
                itens.Property(i => i.ReferenciaId).IsRequired();
                itens.Property(i => i.Descricao).HasMaxLength(200).IsRequired();
                itens.Property(i => i.Quantidade).HasColumnType("numeric(14,3)").IsRequired();
                itens.Property(i => i.PrecoUnitario).HasColumnType("numeric(14,2)").IsRequired();
                itens.Ignore(i => i.Subtotal);
            });
            versoes.Navigation(v => v.Itens).UsePropertyAccessMode(PropertyAccessMode.Field);
        });
        builder.Navigation(o => o.Versoes).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.Ignore(o => o.VersaoVigente);

        builder.Ignore(o => o.EventosDeDominio);
    }
}

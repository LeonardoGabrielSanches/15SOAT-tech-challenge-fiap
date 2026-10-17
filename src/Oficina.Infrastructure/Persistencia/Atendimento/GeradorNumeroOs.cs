using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Oficina.Domain.Atendimento.Entities.ValueObjects;
using Oficina.Domain.Atendimento.Interfaces;

namespace Oficina.Infrastructure.Persistencia.Atendimento;

public sealed class SequenciaAnualDeOs
{
    public int Ano { get; set; }

    public int Ultimo { get; set; }
}

public sealed class SequenciaAnualDeOsConfiguration : IEntityTypeConfiguration<SequenciaAnualDeOs>
{
    public void Configure(EntityTypeBuilder<SequenciaAnualDeOs> builder)
    {
        builder.ToTable("sequencias_anuais_de_os");
        builder.HasKey(s => s.Ano);
        builder.Property(s => s.Ano).ValueGeneratedNever();
        builder.Property(s => s.Ultimo).IsRequired();
    }
}

public sealed class GeradorNumeroOs(DbContext contexto) : IGeradorNumeroOs
{
    public async Task<NumeroOs> Proximo(int ano, CancellationToken cancellationToken)
    {
        var resultado = await contexto.Database
            .SqlQuery<int>($"""
                INSERT INTO sequencias_anuais_de_os ("Ano", "Ultimo") VALUES ({ano}, 1)
                ON CONFLICT ("Ano") DO UPDATE SET "Ultimo" = sequencias_anuais_de_os."Ultimo" + 1
                RETURNING "Ultimo" AS "Value"
                """)
            .ToListAsync(cancellationToken);

        return NumeroOs.Criar(ano, resultado.Single());
    }
}

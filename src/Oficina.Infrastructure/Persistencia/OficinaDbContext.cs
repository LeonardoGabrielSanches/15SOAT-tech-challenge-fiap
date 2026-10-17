using Microsoft.EntityFrameworkCore;

namespace Oficina.Infrastructure.Persistencia;

public sealed class OficinaDbContext(DbContextOptions<OficinaDbContext> opcoes) : DbContext(opcoes)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder) =>
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(OficinaDbContext).Assembly);
}

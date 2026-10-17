using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Oficina.Application.Common;
using Oficina.Infrastructure.Persistencia;

namespace Oficina.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<OficinaDbContext>(o => o.UseNpgsql(connectionString));
        services.AddScoped<DbContext>(sp => sp.GetRequiredService<OficinaDbContext>());
        services.AddScoped<IUnidadeDeTrabalho, UnidadeDeTrabalho>();
        services.AddScoped<IDespachanteDeEventos, DespachanteDeEventos>();
        services.AddSingleton(TimeProvider.System);
        return services;
    }
}

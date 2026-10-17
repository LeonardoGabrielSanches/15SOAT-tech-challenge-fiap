using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Oficina.Application.Common;
using Oficina.Domain.Atendimento.Interfaces;
using Oficina.Infrastructure.Persistencia;
using Oficina.Infrastructure.Persistencia.Atendimento;

namespace Oficina.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<OficinaDbContext>(o => o.UseNpgsql(connectionString));
        services.AddScoped<DbContext>(sp => sp.GetRequiredService<OficinaDbContext>());
        services.AddScoped<IUnidadeDeTrabalho, UnidadeDeTrabalho>();
        services.AddScoped<IDespachanteDeEventos, DespachanteDeEventos>();
        services.AddScoped<IClienteRepository, ClienteRepository>();
        services.AddScoped<IVeiculoRepository, VeiculoRepository>();
        services.AddScoped<IOrdemDeServicoRepository, OrdemDeServicoRepository>();
        services.AddScoped<IGeradorNumeroOs, GeradorNumeroOs>();
        services.AddSingleton<INotificador, NotificadorSimulado>();
        services.AddSingleton(TimeProvider.System);
        return services;
    }
}

using Microsoft.Extensions.DependencyInjection;

namespace Jewel.JPMS.Cqrs;

public static class CqrsServiceRegistration
{
    public static IServiceCollection AddCqrsTransport(this IServiceCollection services)
    {
        services.AddSingleton<CommandRouteTable>();
        services.AddSingleton<QueryRouteTable>();
        services.AddScoped<IQueryClient, HttpQueryClient>();
        services.AddScoped<ICommandSender, HttpCommandSender>();
        return services;
    }
}

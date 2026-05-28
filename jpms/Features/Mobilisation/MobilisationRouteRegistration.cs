using Jewel.JPMS.Contracts.Mobilisation;
using Jewel.JPMS.Cqrs;
using Jewel.JPMS.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Jewel.JPMS.Features.Mobilisation;

public static class MobilisationRouteRegistration
{
    public static IServiceCollection AddMobilisationReadModels(this IServiceCollection services)
    {
        services.AddScoped<MobilisationChecklistReadModel>();
        return services;
    }

    public static void RegisterMobilisationRoutes(QueryRouteTable queries, CommandRouteTable commands)
    {
        queries.Register<GetMobilisationChecklistForProject, MobilisationChecklist>(
            new QueryRoute("/api/projects/{projectId}/mobilisation",
                query => $"/api/projects/{((GetMobilisationChecklistForProject)query).ProjectId}/mobilisation"));

        commands.Register<UpdateMobilisationChecklistItem, MobilisationItem>(
            new CommandRoute("PUT", "/api/mobilisation-items/{mobilisationItemId}",
                command => $"/api/mobilisation-items/{((UpdateMobilisationChecklistItem)command).MobilisationItemId}"));
    }
}

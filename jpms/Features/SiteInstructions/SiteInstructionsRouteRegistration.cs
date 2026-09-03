using Jewel.JPMS.Contracts.SiteInstructions;

namespace Jewel.JPMS.Features.SiteInstructions;

public static class SiteInstructionsRouteRegistration
{
    public static IServiceCollection AddSiteInstructionReadModels(this IServiceCollection services)
    {
        services.AddScoped<SiteInstructionReadModel>();
        return services;
    }

    public static void RegisterSiteInstructionsRoutes(QueryRouteTable queries, CommandRouteTable commands)
    {
        queries.Register<ListSiteInstructionsForProject, IReadOnlyList<SiteInstruction>>(
            new QueryRoute("/api/projects/{projectId}/site-instructions",
                query => $"/api/projects/{((ListSiteInstructionsForProject)query).ProjectId}/site-instructions"));

        commands.Register<AddSiteInstruction, SiteInstruction>(
            new CommandRoute("POST", "/api/projects/{projectId}/site-instructions",
                command => $"/api/projects/{((AddSiteInstruction)command).ProjectId}/site-instructions"));

        commands.Register<UpdateSiteInstruction, SiteInstruction>(
            new CommandRoute("PUT", "/api/site-instructions/{siteInstructionId}",
                command => $"/api/site-instructions/{((UpdateSiteInstruction)command).SiteInstructionId}"));

        // The Control Centre's Internal-pathway "create new → Site instruction": raise + link the
        // originating email.
        commands.Register<CreateSiteInstructionFromMessage, SiteInstruction>(
            new CommandRoute("POST", "/api/mailbox/message/create-site-instruction",
                _ => "/api/mailbox/message/create-site-instruction"));
    }
}

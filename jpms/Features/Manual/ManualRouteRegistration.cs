
namespace Jewel.JPMS.Features.Manual;

/// <summary>Client routes for the site manual. Mirrors the api endpoints under Features/Manual.</summary>
public static class ManualRouteRegistration
{
    public static IServiceCollection AddManualReadModels(this IServiceCollection services)
    {
        services.AddScoped<ManualModulesReadModel>();
        return services;
    }

    public static void RegisterManualRoutes(QueryRouteTable queries, CommandRouteTable commands)
    {
        queries.Register<ListManualModules, IReadOnlyList<ManualModule>>(QueryRoute.Static("/api/manual/modules"));
        queries.Register<GetManualModule, ManualModuleDetail?>(
            new QueryRoute("/api/manual/modules/{manualModuleId}", query => ModuleAddress(((GetManualModule)query).ManualModuleId)));
        queries.Register<GetManualView, ManualViewReading>(
            new QueryRoute("/api/manual/views/{view}", query => $"/api/manual/views/{((GetManualView)query).View}"));

        commands.Register<CreateManualModule, ManualModule>(CommandRoute.Post("/api/manual/modules"));
        commands.Register<ImportManualBaseline, ManualBaselineImport>(CommandRoute.Post("/api/manual/baseline"));
        commands.Register<UpdateManualModuleDraft, ManualModule>(
            new CommandRoute("PUT", "/api/manual/modules/{manualModuleId}", command => ModuleAddress(((UpdateManualModuleDraft)command).ManualModuleId)));
        commands.Register<SubmitManualModuleForReview, ManualModule>(
            Step("submit", command => ((SubmitManualModuleForReview)command).ManualModuleId));
        commands.Register<ApproveManualModule, ManualModule>(
            Step("approve", command => ((ApproveManualModule)command).ManualModuleId));
        commands.Register<ReturnManualModuleToDraft, ManualModule>(
            Step("return", command => ((ReturnManualModuleToDraft)command).ManualModuleId));
        commands.Register<ReviseManualModule, ManualModule>(
            Step("revise", command => ((ReviseManualModule)command).ManualModuleId));
        commands.Register<RetireManualModule, ManualModule>(
            Step("retire", command => ((RetireManualModule)command).ManualModuleId));
        commands.Register<AcknowledgeManualModule, ManualAcknowledgement>(
            Step("acknowledge", command => ((AcknowledgeManualModule)command).ManualModuleId));
    }

    private static string ModuleAddress(string manualModuleId) => $"/api/manual/modules/{manualModuleId}";

    private static CommandRoute Step(string step, Func<object, string> manualModuleIdOf) =>
        new("POST", $"/api/manual/modules/{{manualModuleId}}/{step}", command => $"{ModuleAddress(manualModuleIdOf(command))}/{step}");
}

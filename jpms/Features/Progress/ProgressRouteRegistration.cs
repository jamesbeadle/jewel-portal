using Jewel.JPMS.Contracts.Progress;

namespace Jewel.JPMS.Features.Progress;

public static class ProgressRouteRegistration
{
    public static IServiceCollection AddProgressReadModels(this IServiceCollection services)
    {
        services.AddScoped<ProgressReadModel>();
        return services;
    }

    public static void RegisterProgressRoutes(QueryRouteTable queries, CommandRouteTable commands)
    {
        queries.Register<ListProgressUpdatesForProject, IReadOnlyList<ProgressUpdate>>(
            new QueryRoute("/api/projects/{projectId}/progress-updates",
                query => $"/api/projects/{((ListProgressUpdatesForProject)query).ProjectId}/progress-updates"));

        queries.Register<ListProgressReportsForProject, IReadOnlyList<ProgressReport>>(
            new QueryRoute("/api/projects/{projectId}/progress-reports",
                query => $"/api/projects/{((ListProgressReportsForProject)query).ProjectId}/progress-reports"));

        // Progress update creation and photo additions are multipart/form-data and are sent
        // directly by HttpProgressStore, not via the JSON command sender, so they are
        // intentionally not registered here.

        commands.Register<CreateProgressUpdate, ProgressUpdate>(
            new CommandRoute("POST", "/api/projects/{projectId}/progress-updates/note",
                command => $"/api/projects/{((CreateProgressUpdate)command).ProjectId}/progress-updates/note"));

        commands.Register<UpdateProgressUpdate, ProgressUpdate>(
            new CommandRoute("PUT", "/api/progress-updates/{progressUpdateId}",
                command => $"/api/progress-updates/{((UpdateProgressUpdate)command).ProgressUpdateId}"));

        commands.Register<DeleteProgressUpdate, Acknowledgement>(
            new CommandRoute("DELETE", "/api/progress-updates/{progressUpdateId}",
                command => $"/api/progress-updates/{((DeleteProgressUpdate)command).ProgressUpdateId}"));

        commands.Register<DeleteProgressPhoto, Acknowledgement>(
            new CommandRoute("DELETE", "/api/progress-updates/{progressUpdateId}/photos/{progressPhotoId}",
                command =>
                {
                    var delete = (DeleteProgressPhoto)command;
                    return $"/api/progress-updates/{delete.ProgressUpdateId}/photos/{delete.ProgressPhotoId}";
                }));

        commands.Register<CreateProgressReport, ProgressReport>(
            new CommandRoute("POST", "/api/projects/{projectId}/progress-reports",
                command => $"/api/projects/{((CreateProgressReport)command).ProjectId}/progress-reports"));

        commands.Register<UpdateProgressReport, ProgressReport>(
            new CommandRoute("PUT", "/api/progress-reports/{progressReportId}",
                command => $"/api/progress-reports/{((UpdateProgressReport)command).ProgressReportId}"));

        commands.Register<DeleteProgressReport, Acknowledgement>(
            new CommandRoute("DELETE", "/api/progress-reports/{progressReportId}",
                command => $"/api/progress-reports/{((DeleteProgressReport)command).ProgressReportId}"));

        RegisterContractorsReportRoutes(queries, commands);
        RegisterSitePhotoRoutes(queries, commands);
    }

    private static void RegisterContractorsReportRoutes(QueryRouteTable queries, CommandRouteTable commands)
    {
        queries.Register<ListContractorsReports, IReadOnlyList<ContractorsReport>>(
            new QueryRoute("/api/projects/{projectId}/contractors-reports",
                query => $"/api/projects/{((ListContractorsReports)query).ProjectId}/contractors-reports"));

        queries.Register<GetContractorsReport, ContractorsReportView?>(
            new QueryRoute("/api/contractors-reports/{contractorsReportId}",
                query => $"/api/contractors-reports/{((GetContractorsReport)query).ContractorsReportId}"));

        commands.Register<CreateContractorsReport, ContractorsReport>(
            new CommandRoute("POST", "/api/projects/{projectId}/contractors-reports",
                command => $"/api/projects/{((CreateContractorsReport)command).ProjectId}/contractors-reports"));

        commands.Register<UpdateContractorsReport, ContractorsReport>(
            new CommandRoute("PUT", "/api/contractors-reports/{contractorsReportId}",
                command => $"/api/contractors-reports/{((UpdateContractorsReport)command).ContractorsReportId}"));

        commands.Register<DeleteContractorsReport, Acknowledgement>(
            new CommandRoute("DELETE", "/api/contractors-reports/{contractorsReportId}",
                command => $"/api/contractors-reports/{((DeleteContractorsReport)command).ContractorsReportId}"));
    }

    // The pool's upload is multipart/form-data, sent by HttpSitePhotoStore; the fingerprint match
    // and the filing are the connector's (POST bodies the JSON query sender does not carry), so
    // only the list and the delete are routed here.
    private static void RegisterSitePhotoRoutes(QueryRouteTable queries, CommandRouteTable commands)
    {
        queries.Register<ListSitePhotos, IReadOnlyList<SitePhoto>>(
            new QueryRoute("/api/site-photos",
                query => ((ListSitePhotos)query).UnfiledOnly ? "/api/site-photos?unfiled=1" : "/api/site-photos"));

        commands.Register<DeleteSitePhoto, Acknowledgement>(
            new CommandRoute("DELETE", "/api/site-photos/{sitePhotoId}",
                command => $"/api/site-photos/{((DeleteSitePhoto)command).SitePhotoId}"));
    }
}

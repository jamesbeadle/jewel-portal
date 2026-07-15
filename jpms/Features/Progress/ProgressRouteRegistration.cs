using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Contracts.Progress;
using Jewel.JPMS.Cqrs;
using Jewel.JPMS.Models;
using Microsoft.Extensions.DependencyInjection;

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
    }
}

using Jewel.JPMS.Contracts.Drawings;
using Jewel.JPMS.Cqrs;
using Jewel.JPMS.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Jewel.JPMS.Features.Drawings;

public static class DrawingsRouteRegistration
{
    public static IServiceCollection AddDrawingsReadModels(this IServiceCollection services)
    {
        services.AddScoped<DrawingsReadModel>();
        return services;
    }

    public static void RegisterDrawingsRoutes(QueryRouteTable queries, CommandRouteTable commands)
    {
        queries.Register<ListDrawingsForProject, IReadOnlyList<Drawing>>(
            new QueryRoute("/api/projects/{projectId}/drawings",
                query => $"/api/projects/{((ListDrawingsForProject)query).ProjectId}/drawings"));

        queries.Register<GetDrawingById, Drawing?>(
            new QueryRoute("/api/drawings/{drawingId}",
                query => $"/api/drawings/{((GetDrawingById)query).DrawingId}"));

        queries.Register<ListRevisionsForDrawing, IReadOnlyList<DrawingRevision>>(
            new QueryRoute("/api/drawings/{drawingId}/revisions",
                query => $"/api/drawings/{((ListRevisionsForDrawing)query).DrawingId}/revisions"));

        commands.Register<RegisterDrawing, Drawing>(
            new CommandRoute("POST", "/api/projects/{projectId}/drawings",
                command => $"/api/projects/{((RegisterDrawing)command).ProjectId}/drawings"));

        commands.Register<UpdateDrawingMetadata, Drawing>(
            new CommandRoute("PUT", "/api/drawings/{drawingId}",
                command => $"/api/drawings/{((UpdateDrawingMetadata)command).DrawingId}"));

        // Revision upload is multipart/form-data and is sent directly by HttpDrawingStore, not via
        // the JSON command sender, so it is intentionally not registered here.

        commands.Register<ApproveDrawingRevision, DrawingRevision>(
            new CommandRoute("POST", "/api/drawings/{drawingId}/revisions/{revisionId}/approve",
                command =>
                {
                    var approve = (ApproveDrawingRevision)command;
                    return $"/api/drawings/{approve.DrawingId}/revisions/{approve.DrawingRevisionId}/approve";
                }));
    }
}

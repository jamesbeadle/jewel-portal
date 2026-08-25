using Jewel.JPMS.Contracts.Cqrs;
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

        queries.Register<ListDrawingFoldersForProject, IReadOnlyList<DrawingFolder>>(
            new QueryRoute("/api/projects/{projectId}/drawing-folders",
                query => $"/api/projects/{((ListDrawingFoldersForProject)query).ProjectId}/drawing-folders"));

        commands.Register<CreateDrawingFolder, DrawingFolder>(
            new CommandRoute("POST", "/api/projects/{projectId}/drawing-folders",
                command => $"/api/projects/{((CreateDrawingFolder)command).ProjectId}/drawing-folders"));

        commands.Register<RenameDrawingFolder, DrawingFolder>(
            new CommandRoute("PUT", "/api/drawing-folders/{folderId}",
                command => $"/api/drawing-folders/{((RenameDrawingFolder)command).DrawingFolderId}"));

        commands.Register<DeleteDrawingFolder, Acknowledgement>(
            new CommandRoute("DELETE", "/api/drawing-folders/{folderId}",
                command => $"/api/drawing-folders/{((DeleteDrawingFolder)command).DrawingFolderId}"));

        commands.Register<MoveDrawingToFolder, Drawing>(
            new CommandRoute("PUT", "/api/drawings/{drawingId}/folder",
                command => $"/api/drawings/{((MoveDrawingToFolder)command).DrawingId}/folder"));

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

        commands.Register<SetDrawingRevisionLabel, DrawingRevision>(
            new CommandRoute("PUT", "/api/drawings/{drawingId}/revisions/{revisionId}/label",
                command =>
                {
                    var setLabel = (SetDrawingRevisionLabel)command;
                    return $"/api/drawings/{setLabel.DrawingId}/revisions/{setLabel.DrawingRevisionId}/label";
                }));

        commands.Register<DeleteDrawing, Acknowledgement>(
            new CommandRoute("DELETE", "/api/drawings/{drawingId}",
                command => $"/api/drawings/{((DeleteDrawing)command).DrawingId}"));

        commands.Register<DeleteDrawingRevision, Acknowledgement>(
            new CommandRoute("DELETE", "/api/drawings/{drawingId}/revisions/{revisionId}",
                command =>
                {
                    var delete = (DeleteDrawingRevision)command;
                    return $"/api/drawings/{delete.DrawingId}/revisions/{delete.DrawingRevisionId}";
                }));
    }
}

using Jewel.JPMS.Api.Features.Drawings.Commands;
using Jewel.JPMS.Api.Features.Drawings.Queries;
using Jewel.JPMS.Api.Features.Drawings.Storage;
using Jewel.JPMS.Contracts.Drawings;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Jewel.JPMS.Api.Features.Drawings;

public static class DrawingsFeatureRegistration
{
    public static IServiceCollection AddDrawingsFeature(this IServiceCollection services, IConfiguration configuration)
    {
        RegisterBlobStore(services, configuration);

        services.AddScoped<IQueryHandler<ListDrawingsForProject, IReadOnlyList<Drawing>>, ListDrawingsForProjectHandler>();
        services.AddScoped<IQueryHandler<GetDrawingById, Drawing?>, GetDrawingByIdHandler>();
        services.AddScoped<IQueryHandler<ListRevisionsForDrawing, IReadOnlyList<DrawingRevision>>, ListRevisionsForDrawingHandler>();

        services.AddScoped<ICommandHandler<RegisterDrawing, Drawing>, RegisterDrawingHandler>();
        services.AddScoped<RegisterDrawingAuthorisation>();
        services.AddScoped<RegisterDrawingValidation>();

        services.AddScoped<ICommandHandler<UpdateDrawingMetadata, Drawing>, UpdateDrawingMetadataHandler>();
        services.AddScoped<UpdateDrawingMetadataAuthorisation>();
        services.AddScoped<UpdateDrawingMetadataValidation>();

        services.AddScoped<ICommandHandler<UploadDrawingRevision, DrawingRevision>, UploadDrawingRevisionHandler>();
        services.AddScoped<UploadDrawingRevisionAuthorisation>();
        services.AddScoped<UploadDrawingRevisionValidation>();

        services.AddScoped<ICommandHandler<SetDrawingRevisionLabel, DrawingRevision>, SetDrawingRevisionLabelHandler>();
        services.AddScoped<SetDrawingRevisionLabelAuthorisation>();
        services.AddScoped<SetDrawingRevisionLabelValidation>();

        services.AddScoped<ICommandHandler<ApproveDrawingRevision, DrawingRevision>, ApproveDrawingRevisionHandler>();
        services.AddScoped<ApproveDrawingRevisionAuthorisation>();
        services.AddScoped<ApproveDrawingRevisionValidation>();

        services.AddScoped<ICommandHandler<DeleteDrawing, Acknowledgement>, DeleteDrawingHandler>();
        services.AddScoped<DeleteDrawingAuthorisation>();
        services.AddScoped<DeleteDrawingValidation>();

        services.AddScoped<ICommandHandler<DeleteDrawingRevision, Acknowledgement>, DeleteDrawingRevisionHandler>();
        services.AddScoped<DeleteDrawingRevisionAuthorisation>();
        services.AddScoped<DeleteDrawingRevisionValidation>();

        services.AddScoped<IQueryHandler<ListDrawingFoldersForProject, IReadOnlyList<DrawingFolder>>, ListDrawingFoldersForProjectHandler>();

        services.AddScoped<ICommandHandler<CreateDrawingFolder, DrawingFolder>, CreateDrawingFolderHandler>();
        services.AddScoped<CreateDrawingFolderAuthorisation>();
        services.AddScoped<CreateDrawingFolderValidation>();

        services.AddScoped<ICommandHandler<RenameDrawingFolder, DrawingFolder>, RenameDrawingFolderHandler>();
        services.AddScoped<RenameDrawingFolderAuthorisation>();
        services.AddScoped<RenameDrawingFolderValidation>();

        services.AddScoped<ICommandHandler<DeleteDrawingFolder, Acknowledgement>, DeleteDrawingFolderHandler>();
        services.AddScoped<DeleteDrawingFolderAuthorisation>();
        services.AddScoped<DeleteDrawingFolderValidation>();

        services.AddScoped<ICommandHandler<MoveDrawingToFolder, Drawing>, MoveDrawingToFolderHandler>();
        services.AddScoped<MoveDrawingToFolderAuthorisation>();
        services.AddScoped<MoveDrawingToFolderValidation>();

        return services;
    }

    private static void RegisterBlobStore(IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration["DrawingsStorage:ConnectionString"]
            ?? configuration["AzureWebJobsStorage"];

        if (string.IsNullOrWhiteSpace(connectionString))
            services.AddSingleton<IDrawingBlobStore, NullDrawingBlobStore>();
        else
            services.AddSingleton<IDrawingBlobStore>(_ => new AzureBlobDrawingStore(connectionString));
    }
}

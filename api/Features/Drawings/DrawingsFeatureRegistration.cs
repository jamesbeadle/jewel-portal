using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Features.Drawings.Commands;
using Jewel.JPMS.Api.Features.Drawings.Queries;
using Jewel.JPMS.Api.Features.Drawings.Storage;
using Jewel.JPMS.Contracts.Drawings;
using Jewel.JPMS.Models;
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

        services.AddScoped<ICommandHandler<ApproveDrawingRevision, DrawingRevision>, ApproveDrawingRevisionHandler>();
        services.AddScoped<ApproveDrawingRevisionAuthorisation>();
        services.AddScoped<ApproveDrawingRevisionValidation>();

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

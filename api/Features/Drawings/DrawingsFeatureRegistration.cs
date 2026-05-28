using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Features.Drawings.Commands;
using Jewel.JPMS.Api.Features.Drawings.Queries;
using Jewel.JPMS.Contracts.Drawings;
using Jewel.JPMS.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Jewel.JPMS.Api.Features.Drawings;

public static class DrawingsFeatureRegistration
{
    public static IServiceCollection AddDrawingsFeature(this IServiceCollection services)
    {
        services.AddScoped<IQueryHandler<ListDrawingsForProject, IReadOnlyList<Drawing>>, ListDrawingsForProjectHandler>();
        services.AddScoped<IQueryHandler<GetDrawingById, Drawing?>, GetDrawingByIdHandler>();
        services.AddScoped<IQueryHandler<ListRevisionsForDrawing, IReadOnlyList<DrawingRevision>>, ListRevisionsForDrawingHandler>();

        services.AddScoped<ICommandHandler<RegisterDrawing, Drawing>, RegisterDrawingHandler>();
        services.AddScoped<RegisterDrawingAuthorisation>();
        services.AddScoped<RegisterDrawingValidation>();

        services.AddScoped<ICommandHandler<UpdateDrawingMetadata, Drawing>, UpdateDrawingMetadataHandler>();
        services.AddScoped<UpdateDrawingMetadataAuthorisation>();
        services.AddScoped<UpdateDrawingMetadataValidation>();

        services.AddScoped<ICommandHandler<IssueDrawingRevision, DrawingRevision>, IssueDrawingRevisionHandler>();
        services.AddScoped<IssueDrawingRevisionAuthorisation>();
        services.AddScoped<IssueDrawingRevisionValidation>();

        return services;
    }
}

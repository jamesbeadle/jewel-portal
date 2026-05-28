using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Features.Subcontractors.Commands;
using Jewel.JPMS.Api.Features.Subcontractors.Queries;
using Jewel.JPMS.Contracts.Subcontractors;
using Jewel.JPMS.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Jewel.JPMS.Api.Features.Subcontractors;

public static class SubcontractorsFeatureRegistration
{
    public static IServiceCollection AddSubcontractorsFeature(this IServiceCollection services)
    {
        services.AddScoped<IQueryHandler<ListSubcontractors, IReadOnlyList<Subcontractor>>, ListSubcontractorsHandler>();
        services.AddScoped<IQueryHandler<ListComplianceDocumentsForSubcontractor, IReadOnlyList<ComplianceDocument>>, ListComplianceDocumentsForSubcontractorHandler>();

        services.AddScoped<ICommandHandler<AddSubcontractorToDirectory, Subcontractor>, AddSubcontractorToDirectoryHandler>();
        services.AddScoped<AddSubcontractorToDirectoryAuthorisation>();
        services.AddScoped<AddSubcontractorToDirectoryValidation>();

        services.AddScoped<ICommandHandler<UpdateSubcontractor, Subcontractor>, UpdateSubcontractorHandler>();
        services.AddScoped<UpdateSubcontractorAuthorisation>();
        services.AddScoped<UpdateSubcontractorValidation>();

        services.AddScoped<ICommandHandler<UploadComplianceDocument, ComplianceDocument>, UploadComplianceDocumentHandler>();
        services.AddScoped<UploadComplianceDocumentAuthorisation>();
        services.AddScoped<UploadComplianceDocumentValidation>();

        return services;
    }
}

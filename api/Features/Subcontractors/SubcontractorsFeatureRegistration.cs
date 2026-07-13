using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Features.Subcontractors.Commands;
using Jewel.JPMS.Api.Features.Subcontractors.Queries;
using Jewel.JPMS.Api.Features.Subcontractors.Storage;
using Jewel.JPMS.Contracts.Subcontractors;
using Jewel.JPMS.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Jewel.JPMS.Api.Features.Subcontractors;

public static class SubcontractorsFeatureRegistration
{
    public static IServiceCollection AddSubcontractorsFeature(this IServiceCollection services, IConfiguration configuration)
    {
        RegisterBlobStore(services, configuration);

        services.AddScoped<IQueryHandler<ListSubcontractors, IReadOnlyList<Subcontractor>>, ListSubcontractorsHandler>();
        services.AddScoped<IQueryHandler<ListTrades, IReadOnlyList<Trade>>, ListTradesHandler>();
        services.AddScoped<IQueryHandler<ListComplianceDocumentsForSubcontractor, IReadOnlyList<ComplianceDocument>>, ListComplianceDocumentsForSubcontractorHandler>();

        services.AddScoped<ICommandHandler<AddSubcontractorToDirectory, Subcontractor>, AddSubcontractorToDirectoryHandler>();
        services.AddScoped<AddSubcontractorToDirectoryAuthorisation>();
        services.AddScoped<AddSubcontractorToDirectoryValidation>();

        services.AddScoped<ICommandHandler<UpdateSubcontractor, Subcontractor>, UpdateSubcontractorHandler>();
        services.AddScoped<UpdateSubcontractorAuthorisation>();
        services.AddScoped<UpdateSubcontractorValidation>();

        services.AddScoped<ICommandHandler<AddTrade, Trade>, AddTradeHandler>();
        services.AddScoped<AddTradeAuthorisation>();
        services.AddScoped<AddTradeValidation>();

        services.AddScoped<ICommandHandler<UploadComplianceDocument, ComplianceDocument>, UploadComplianceDocumentHandler>();
        services.AddScoped<UploadComplianceDocumentAuthorisation>();
        services.AddScoped<UploadComplianceDocumentValidation>();

        services.AddScoped<ICommandHandler<AddComplianceDocumentVersion, ComplianceDocument>, AddComplianceDocumentVersionHandler>();

        services.AddScoped<SubcontractorPortalInviter>();
        services.AddScoped<InviteSubcontractorPortalUserAuthorisation>();

        return services;
    }

    // Mirrors the drawings feature: private container, loud NullStore when unconfigured.
    private static void RegisterBlobStore(IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration["ComplianceStorage:ConnectionString"]
            ?? configuration["AzureWebJobsStorage"];

        if (string.IsNullOrWhiteSpace(connectionString))
            services.AddSingleton<IComplianceBlobStore, NullComplianceBlobStore>();
        else
            services.AddSingleton<IComplianceBlobStore>(_ => new AzureBlobComplianceStore(connectionString));
    }
}

using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Features.Subcontractors.Commands;
using Jewel.JPMS.Api.Features.Subcontractors.Queries;
using Jewel.JPMS.Api.Features.Subcontractors.Storage;
using Jewel.JPMS.Contracts.Cqrs;
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
        services.AddScoped<IQueryHandler<GetSubcontractorStatement, SubcontractorStatement>, GetSubcontractorStatementHandler>();

        services.AddScoped<ICommandHandler<PrepareSubcontractorStatementEmailDraft, SubcontractorStatementEmailDraft>, PrepareSubcontractorStatementEmailDraftHandler>();
        services.AddScoped<PrepareSubcontractorStatementEmailDraftAuthorisation>();
        services.AddScoped<PrepareSubcontractorStatementEmailDraftValidation>();

        services.AddScoped<ICommandHandler<AddSubcontractorToDirectory, Subcontractor>, AddSubcontractorToDirectoryHandler>();
        services.AddScoped<AddSubcontractorToDirectoryAuthorisation>();
        services.AddScoped<AddSubcontractorToDirectoryValidation>();

        services.AddScoped<ICommandHandler<UpdateSubcontractor, Subcontractor>, UpdateSubcontractorHandler>();
        services.AddScoped<UpdateSubcontractorAuthorisation>();
        services.AddScoped<UpdateSubcontractorValidation>();

        services.AddScoped<ICommandHandler<AddTrade, Trade>, AddTradeHandler>();
        services.AddScoped<AddTradeAuthorisation>();
        services.AddScoped<AddTradeValidation>();

        services.AddScoped<ICommandHandler<RenameTrade, Trade>, RenameTradeHandler>();
        services.AddScoped<RenameTradeAuthorisation>();
        services.AddScoped<RenameTradeValidation>();

        services.AddScoped<ICommandHandler<DeleteTrade, Acknowledgement>, DeleteTradeHandler>();
        services.AddScoped<DeleteTradeAuthorisation>();
        services.AddScoped<DeleteTradeValidation>();

        services.AddScoped<ICommandHandler<PromoteSubcontractorToDirectory, Subcontractor>, PromoteSubcontractorToDirectoryHandler>();
        services.AddScoped<PromoteSubcontractorToDirectoryAuthorisation>();
        services.AddScoped<PromoteSubcontractorToDirectoryValidation>();

        services.AddScoped<ICommandHandler<UploadComplianceDocument, ComplianceDocument>, UploadComplianceDocumentHandler>();
        services.AddScoped<UploadComplianceDocumentAuthorisation>();
        services.AddScoped<UploadComplianceDocumentValidation>();
        services.AddScoped<UploadComplianceDocumentFileAuthorisation>();

        services.AddScoped<ICommandHandler<AddComplianceDocumentVersion, ComplianceDocument>, AddComplianceDocumentVersionHandler>();

        // Xero import + consolidation (the duplicate-resolution flow) + company contacts.
        services.AddScoped<ICommandHandler<ImportXeroSupplier, Subcontractor>, ImportXeroSupplierHandler>();
        services.AddScoped<ImportXeroSupplierAuthorisation>();
        services.AddScoped<ImportXeroSupplierValidation>();

        services.AddScoped<ICommandHandler<ConsolidateDirectoryRecords, Subcontractor>, ConsolidateDirectoryRecordsHandler>();
        services.AddScoped<ConsolidateDirectoryRecordsAuthorisation>();
        services.AddScoped<ConsolidateDirectoryRecordsValidation>();

        services.AddScoped<IQueryHandler<ListCompanyContacts, IReadOnlyList<CompanyContact>>, ListCompanyContactsHandler>();
        services.AddScoped<ICommandHandler<UpsertCompanyContact, CompanyContact>, UpsertCompanyContactHandler>();
        services.AddScoped<ICommandHandler<RemoveCompanyContact, Acknowledgement>, RemoveCompanyContactHandler>();
        services.AddScoped<UpsertCompanyContactAuthorisation>();
        services.AddScoped<UpsertCompanyContactValidation>();

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

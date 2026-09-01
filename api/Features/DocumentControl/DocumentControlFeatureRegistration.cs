using Jewel.JPMS.Api.Features.DocumentControl.Commands;
using Jewel.JPMS.Api.Features.DocumentControl.Queries;
using Jewel.JPMS.Api.Features.DocumentControl.Storage;
using Jewel.JPMS.Contracts.DocumentControl;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Jewel.JPMS.Api.Features.DocumentControl;

public static class DocumentControlFeatureRegistration
{
    public static IServiceCollection AddDocumentControlFeature(this IServiceCollection services, IConfiguration configuration)
    {
        RegisterBlobStore(services, configuration);

        services.AddScoped<IQueryHandler<ListDocumentControlItems, IReadOnlyList<DocumentControlItem>>, ListDocumentControlItemsHandler>();
        services.AddScoped<IQueryHandler<ListPaymentCertificates, IReadOnlyList<PaymentCertificate>>, ListPaymentCertificatesHandler>();

        services.AddScoped<ICommandHandler<SendAttachmentsToDocumentControl, IReadOnlyList<DocumentControlItem>>, SendAttachmentsToDocumentControlHandler>();
        services.AddScoped<SendAttachmentsToDocumentControlAuthorisation>();
        services.AddScoped<SendAttachmentsToDocumentControlValidation>();

        services.AddScoped<ICommandHandler<FileDocumentAsDrawing, DocumentControlItem>, FileDocumentAsDrawingHandler>();
        services.AddScoped<FileDocumentAsDrawingAuthorisation>();
        services.AddScoped<FileDocumentAsDrawingValidation>();

        services.AddScoped<ICommandHandler<FileDocumentAsPaymentCertificate, DocumentControlItem>, FileDocumentAsPaymentCertificateHandler>();
        services.AddScoped<FileDocumentAsPaymentCertificateAuthorisation>();
        services.AddScoped<FileDocumentAsPaymentCertificateValidation>();

        services.AddScoped<ICommandHandler<FileDocumentToSubcontractor, DocumentControlItem>, FileDocumentToSubcontractorHandler>();
        services.AddScoped<FileDocumentToSubcontractorAuthorisation>();
        services.AddScoped<FileDocumentToSubcontractorValidation>();

        services.AddScoped<ICommandHandler<DiscardDocumentControlItem, DocumentControlItem>, DiscardDocumentControlItemHandler>();
        services.AddScoped<ICommandHandler<RestoreDocumentControlItem, DocumentControlItem>, RestoreDocumentControlItemHandler>();

        services.AddScoped<ICommandHandler<ExtractDocumentControlArchive, IReadOnlyList<DocumentControlItem>>, ExtractDocumentControlArchiveHandler>();
        services.AddScoped<ExtractDocumentControlArchiveAuthorisation>();
        services.AddScoped<ExtractDocumentControlArchiveValidation>();

        return services;
    }

    private static void RegisterBlobStore(IServiceCollection services, IConfiguration configuration)
    {
        // Same account as the drawings store unless its own is configured (mirrors
        // DrawingsFeatureRegistration): a dedicated container in the shared storage account.
        var connectionString = configuration["DocumentControlStorage:ConnectionString"]
            ?? configuration["DrawingsStorage:ConnectionString"]
            ?? configuration["AzureWebJobsStorage"];

        if (string.IsNullOrWhiteSpace(connectionString))
            services.AddSingleton<IDocumentControlBlobStore, NullDocumentControlBlobStore>();
        else
            services.AddSingleton<IDocumentControlBlobStore>(_ => new AzureBlobDocumentControlStore(connectionString));
    }
}

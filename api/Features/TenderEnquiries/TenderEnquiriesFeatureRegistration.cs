using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Features.TenderEnquiries.Attachments;
using Jewel.JPMS.Api.Features.TenderEnquiries.Commands;
using Jewel.JPMS.Api.Features.TenderEnquiries.Queries;
using Jewel.JPMS.Contracts.TenderEnquiries;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Jewel.JPMS.Api.Features.TenderEnquiries;

public static class TenderEnquiriesFeatureRegistration
{
    public static IServiceCollection AddTenderEnquiriesFeature(this IServiceCollection services, IConfiguration configuration)
    {
        RegisterAttachmentStore(services, configuration);

        // The pieces the two logging routes share: the Lead project shell, the enquiry row +
        // audit, and the one way a file lands on an enquiry.
        services.AddScoped<TenderEnquiryProjectCreator>();
        services.AddScoped<TenderEnquiryProjectResolver>();
        services.AddScoped<TenderEnquiryRegister>();
        services.AddScoped<TenderEnquiryAttachmentWriter>();
        services.AddScoped<TenderEnquiryEmailAttachmentFetcher>();

        services.AddScoped<ICommandHandler<LogTenderEnquiryFromMessage, TenderEnquiry>, LogTenderEnquiryFromMessageHandler>();
        services.AddScoped<LogTenderEnquiryFromMessageAuthorisation>();
        services.AddScoped<LogTenderEnquiryFromMessageValidation>();

        services.AddScoped<ICommandHandler<LogTenderEnquiry, TenderEnquiry>, LogTenderEnquiryHandler>();
        services.AddScoped<LogTenderEnquiryAuthorisation>();
        services.AddScoped<LogTenderEnquiryValidation>();

        services.AddScoped<ICommandHandler<UpdateTenderEnquiryDetails, TenderEnquiry>, UpdateTenderEnquiryDetailsHandler>();
        services.AddScoped<UpdateTenderEnquiryDetailsAuthorisation>();
        services.AddScoped<UpdateTenderEnquiryDetailsValidation>();

        services.AddScoped<ICommandHandler<SetTenderEnquiryStatus, TenderEnquiry>, SetTenderEnquiryStatusHandler>();
        services.AddScoped<SetTenderEnquiryStatusAuthorisation>();
        services.AddScoped<SetTenderEnquiryStatusValidation>();

        services.AddScoped<ICommandHandler<SetTenderEnquiryAnswers, IReadOnlyList<TenderEnquiryAnswer>>, SetTenderEnquiryAnswersHandler>();
        services.AddScoped<SetTenderEnquiryAnswersAuthorisation>();
        services.AddScoped<SetTenderEnquiryAnswersValidation>();

        services.AddScoped<IQueryHandler<ListTenderEnquiries, IReadOnlyList<TenderEnquiry>>, ListTenderEnquiriesHandler>();
        services.AddScoped<IQueryHandler<ListTenderEnquiriesForProject, IReadOnlyList<TenderEnquiry>>, ListTenderEnquiriesForProjectHandler>();
        services.AddScoped<IQueryHandler<GetTenderEnquiryById, TenderEnquiry?>, GetTenderEnquiryByIdHandler>();
        services.AddScoped<IQueryHandler<ListTenderEnquiryAnswers, IReadOnlyList<TenderEnquiryAnswer>>, ListTenderEnquiryAnswersHandler>();
        services.AddScoped<IQueryHandler<GetTenderEnquiryDocument, TenderEnquiryDocumentFile?>, GetTenderEnquiryDocumentHandler>();

        services.AddScoped<IQueryHandler<ListTenderEnquiryAttachments, IReadOnlyList<TenderEnquiryAttachment>>, ListTenderEnquiryAttachmentsHandler>();
        services.AddScoped<ICommandHandler<RemoveTenderEnquiryAttachment, IReadOnlyList<TenderEnquiryAttachment>>, RemoveTenderEnquiryAttachmentHandler>();

        return services;
    }

    // Same connection chain as the other document stores, with its own key first so tender
    // documents can be split onto their own account if volume ever warrants it.
    private static void RegisterAttachmentStore(IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration["TenderEnquiryAttachmentsStorage:ConnectionString"]
            ?? configuration["DrawingsStorage:ConnectionString"]
            ?? configuration["AzureWebJobsStorage"];

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            services.AddSingleton<ITenderEnquiryAttachmentStore, NullTenderEnquiryAttachmentStore>();
            return;
        }
        services.AddSingleton<ITenderEnquiryAttachmentStore>(_ => new AzureBlobTenderEnquiryAttachmentStore(connectionString));
    }
}

using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Features.ValuationInvoices.Commands;
using Jewel.JPMS.Api.Features.ValuationInvoices.Queries;
using Jewel.JPMS.Contracts.ValuationInvoices;
using Jewel.JPMS.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Jewel.JPMS.Api.Features.ValuationInvoices;

public static class ValuationInvoicesFeatureRegistration
{
    public static IServiceCollection AddValuationInvoicesFeature(this IServiceCollection services)
    {
        services.AddScoped<IQueryHandler<ListValuationInvoicesForProject, IReadOnlyList<ValuationInvoice>>, ListValuationInvoicesForProjectHandler>();
        services.AddScoped<IQueryHandler<GetProjectValuationInvoiceSummary, ProjectValuationInvoiceSummary>, GetProjectValuationInvoiceSummaryHandler>();

        services.AddScoped<ICommandHandler<CreateValuationInvoice, ValuationInvoice>, CreateValuationInvoiceHandler>();
        services.AddScoped<CreateValuationInvoiceAuthorisation>();
        services.AddScoped<CreateValuationInvoiceValidation>();

        services.AddScoped<ICommandHandler<IssueValuationInvoice, ValuationInvoice>, IssueValuationInvoiceHandler>();
        services.AddScoped<IssueValuationInvoiceAuthorisation>();
        services.AddScoped<IssueValuationInvoiceValidation>();

        services.AddScoped<ICommandHandler<RecordValuationInvoicePayment, ValuationInvoice>, RecordValuationInvoicePaymentHandler>();
        services.AddScoped<RecordValuationInvoicePaymentAuthorisation>();
        services.AddScoped<RecordValuationInvoicePaymentValidation>();

        return services;
    }
}

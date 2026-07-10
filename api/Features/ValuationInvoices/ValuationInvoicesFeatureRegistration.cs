using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Features.ValuationInvoices.Commands;
using Jewel.JPMS.Api.Features.ValuationInvoices.Queries;
using Jewel.JPMS.Contracts.Cqrs;
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
        services.AddScoped<IQueryHandler<ListValuationInvoiceEvents, IReadOnlyList<ValuationInvoiceEvent>>, ListValuationInvoiceEventsHandler>();

        services.AddScoped<ICommandHandler<CreateValuationInvoice, ValuationInvoice>, CreateValuationInvoiceHandler>();
        services.AddScoped<CreateValuationInvoiceAuthorisation>();
        services.AddScoped<CreateValuationInvoiceValidation>();

        services.AddScoped<ICommandHandler<IssueValuationInvoice, ValuationInvoice>, IssueValuationInvoiceHandler>();
        services.AddScoped<IssueValuationInvoiceAuthorisation>();
        services.AddScoped<IssueValuationInvoiceValidation>();

        services.AddScoped<ICommandHandler<RecordValuationInvoicePayment, ValuationInvoice>, RecordValuationInvoicePaymentHandler>();
        services.AddScoped<RecordValuationInvoicePaymentAuthorisation>();
        services.AddScoped<RecordValuationInvoicePaymentValidation>();

        services.AddScoped<ICommandHandler<DeleteValuationInvoice, Acknowledgement>, DeleteValuationInvoiceHandler>();
        services.AddScoped<DeleteValuationInvoiceAuthorisation>();

        // Approval workflow + amendment — Submit/Approve/Reject/Cancel/Update share one
        // authorisation surface (same roles as raising an invoice).
        services.AddScoped<ValuationInvoiceWorkflowAuthorisation>();

        services.AddScoped<ICommandHandler<SubmitValuationInvoice, ValuationInvoice>, SubmitValuationInvoiceHandler>();
        services.AddScoped<ICommandHandler<ApproveValuationInvoice, ValuationInvoice>, ApproveValuationInvoiceHandler>();

        services.AddScoped<ICommandHandler<RejectValuationInvoice, ValuationInvoice>, RejectValuationInvoiceHandler>();
        services.AddScoped<RejectValuationInvoiceValidation>();

        services.AddScoped<ICommandHandler<CancelValuationInvoice, ValuationInvoice>, CancelValuationInvoiceHandler>();

        services.AddScoped<ICommandHandler<UpdateValuationInvoice, ValuationInvoice>, UpdateValuationInvoiceHandler>();
        services.AddScoped<UpdateValuationInvoiceValidation>();

        return services;
    }
}

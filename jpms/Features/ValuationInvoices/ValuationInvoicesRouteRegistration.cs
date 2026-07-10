using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Contracts.ValuationInvoices;
using Jewel.JPMS.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Features.ValuationInvoices;

public static class ValuationInvoicesRouteRegistration
{
    public static void RegisterValuationInvoicesRoutes(QueryRouteTable queries, CommandRouteTable commands)
    {
        queries.Register<ListValuationInvoicesForProject, IReadOnlyList<ValuationInvoice>>(
            new QueryRoute("/api/projects/{projectId}/valuation-invoices",
                query => $"/api/projects/{((ListValuationInvoicesForProject)query).ProjectId}/valuation-invoices"));

        queries.Register<GetProjectValuationInvoiceSummary, ProjectValuationInvoiceSummary>(
            new QueryRoute("/api/projects/{projectId}/valuation-invoices/summary",
                query => $"/api/projects/{((GetProjectValuationInvoiceSummary)query).ProjectId}/valuation-invoices/summary"));

        commands.Register<CreateValuationInvoice, ValuationInvoice>(
            new CommandRoute("POST", "/api/projects/{projectId}/valuation-invoices",
                command => $"/api/projects/{((CreateValuationInvoice)command).ProjectId}/valuation-invoices"));

        commands.Register<IssueValuationInvoice, ValuationInvoice>(
            new CommandRoute("POST", "/api/valuation-invoices/{valuationInvoiceId}/issue",
                command => $"/api/valuation-invoices/{((IssueValuationInvoice)command).ValuationInvoiceId}/issue"));

        commands.Register<RecordValuationInvoicePayment, ValuationInvoice>(
            new CommandRoute("POST", "/api/valuation-invoices/{valuationInvoiceId}/payment",
                command => $"/api/valuation-invoices/{((RecordValuationInvoicePayment)command).ValuationInvoiceId}/payment"));

        commands.Register<DeleteValuationInvoice, Acknowledgement>(
            new CommandRoute("DELETE", "/api/valuation-invoices/{valuationInvoiceId}",
                command => $"/api/valuation-invoices/{((DeleteValuationInvoice)command).ValuationInvoiceId}"));
    }
}

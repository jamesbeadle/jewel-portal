using Jewel.JPMS.Contracts.Xero;
using Jewel.JPMS.Cqrs;
using Microsoft.Extensions.DependencyInjection;

namespace Jewel.JPMS.Features.Xero;

public static class XeroRouteRegistration
{
    public static IServiceCollection AddXeroReadModels(this IServiceCollection services)
    {
        services.AddScoped<XeroTransactionsReadModel>();
        services.AddScoped<XeroLedgerReadModel>();
        return services;
    }

    public static void RegisterXeroRoutes(QueryRouteTable queries, CommandRouteTable commands)
    {
        queries.Register<ListXeroTransactions, XeroTransactionsSnapshot>(
            new QueryRoute("/api/xero/transactions",
                query => ((ListXeroTransactions)query).Force ? "/api/xero/transactions?force=true" : "/api/xero/transactions"));

        queries.Register<ListXeroLedgerLines, IReadOnlyList<XeroLedgerLine>>(QueryRoute.Static("/api/xero/ledger"));

        queries.Register<ListXeroInvoiceAttachments, IReadOnlyList<XeroInvoiceAttachment>>(
            new QueryRoute("/api/xero/invoice/attachments", query =>
            {
                var attachments = (ListXeroInvoiceAttachments)query;
                return $"/api/xero/invoice/attachments?id={Uri.EscapeDataString(attachments.XeroInvoiceId)}"
                       + (attachments.IsCreditNote ? "&credit=1" : "");
            }));

        commands.Register<SyncXeroLedger, XeroLedgerSyncResult>(CommandRoute.Post("/api/xero/ledger/sync"));
        commands.Register<SetXeroAllocation, int>(CommandRoute.Post("/api/xero/allocations"));
        commands.Register<AllocateSuggestedXeroLines, int>(CommandRoute.Post("/api/xero/allocations/suggested"));
        commands.Register<RetryXeroWriteBack, XeroWriteBackOutcome>(CommandRoute.Post("/api/xero/writeback/retry"));
    }
}

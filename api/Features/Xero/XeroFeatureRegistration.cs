using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Features.Xero.Ledger;
using Jewel.JPMS.Api.Features.Xero.Queries;
using Jewel.JPMS.Contracts.Xero;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Jewel.JPMS.Api.Features.Xero;

/// <summary>
/// Registers the Xero ledger read used for financial reconciliation. Real client when a Xero custom
/// connection's client id/secret are present in configuration, otherwise a no-op so the rest of the
/// app runs unchanged and the ledger UI explains that the credentials are missing. The credentials
/// are read from app settings / Key Vault only (Xero__ClientId, Xero__ClientSecret) — never from
/// source control.
/// </summary>
public static class XeroFeatureRegistration
{
    public static IServiceCollection AddXeroFeature(this IServiceCollection services, IConfiguration configuration)
    {
        var options = XeroOptions.FromConfiguration(configuration);
        services.AddSingleton(options);

        if (options.IsConfigured)
        {
            // Own HttpClient instance so it doesn't clash with the Graph client's registration.
            // Singleton so the cached access token is shared across requests.
            services.AddSingleton<IXeroClient>(sp =>
                new XeroClient(new HttpClient(), options, sp.GetRequiredService<ILogger<XeroClient>>()));
        }
        else
        {
            services.AddSingleton<IXeroClient, NullXeroClient>();
        }

        services.AddScoped<IQueryHandler<ListXeroTransactions, XeroTransactionsSnapshot>, ListXeroTransactionsHandler>();

        // Cash summary: bank balances + outstanding sales invoices for the company Cash Summary page.
        services.AddScoped<IQueryHandler<GetXeroCashSummary, XeroCashSummarySnapshot>, GetXeroCashSummaryHandler>();

        // Ledger allocation: stored Xero lines reconciled onto projects + master cost centres.
        services.AddScoped<ICommandHandler<SyncXeroLedger, XeroLedgerSyncResult>, SyncXeroLedgerHandler>();
        services.AddScoped<IQueryHandler<ListXeroLedgerLines, IReadOnlyList<XeroLedgerLine>>, ListXeroLedgerLinesHandler>();
        services.AddScoped<ICommandHandler<SetXeroAllocation, int>, SetXeroAllocationHandler>();
        services.AddScoped<ICommandHandler<AllocateSuggestedXeroLines, int>, AllocateSuggestedXeroLinesHandler>();
        services.AddScoped<IQueryHandler<ListXeroInvoiceAttachments, IReadOnlyList<XeroInvoiceAttachment>>,
            ListXeroInvoiceAttachmentsHandler>();

        // Write-back: once a draft bill's lines are all allocated, its Sites/Cost Code
        // tracking is confirmed onto the Xero invoice and the invoice is approved.
        services.AddScoped<IXeroWriteBackService, XeroWriteBackService>();
        services.AddScoped<ICommandHandler<RetryXeroWriteBack, XeroWriteBackOutcome>, RetryXeroWriteBackHandler>();

        return services;
    }
}

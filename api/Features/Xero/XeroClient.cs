using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Jewel.JPMS.Contracts.Xero;
using Microsoft.Extensions.Logging;

namespace Jewel.JPMS.Api.Features.Xero;
/// <summary>REST implementation (hand-rolled HttpClient, matching the app's style — see ClaudeClient).</summary>
public sealed partial class XeroClient : IXeroClient
{
    private const string TokenUrl = "https://identity.xero.com/connect/token";
    private const string InvoicesUrl = "https://api.xero.com/api.xro/2.0/Invoices";
    private const string CreditNotesUrl = "https://api.xero.com/api.xro/2.0/CreditNotes";
    private const string AccountsUrl = "https://api.xero.com/api.xro/2.0/Accounts";
    private const string ContactsUrl = "https://api.xero.com/api.xro/2.0/Contacts";
    private const string BankSummaryReportUrl = "https://api.xero.com/api.xro/2.0/Reports/BankSummary";
    private const string ProfitAndLossReportUrl = "https://api.xero.com/api.xro/2.0/Reports/ProfitAndLoss";
    private const string TrackingCategoriesUrl = "https://api.xero.com/api.xro/2.0/TrackingCategories";
    private const int PageSize = 100; // Xero's page size for the Invoices endpoint.

    private readonly HttpClient _http;
    private readonly XeroOptions _options;
    private readonly ILogger<XeroClient> _logger;

    // Client-credentials tokens last ~30 minutes; cache until shortly before expiry.
    private readonly SemaphoreSlim _tokenLock = new(1, 1);
    private string? _accessToken;
    private DateTimeOffset _accessTokenExpiresAt = DateTimeOffset.MinValue;

    // Snapshot cache — one fetch serves every user for CacheMinutes. Guarded by a lock so two
    // simultaneous page loads don't both run a multi-page Xero read.
    private readonly SemaphoreSlim _snapshotLock = new(1, 1);
    private XeroTransactionsSnapshot? _cachedSnapshot;
    private DateTimeOffset _cachedSnapshotAt = DateTimeOffset.MinValue;

    // Cash summary cache — separate from the transactions snapshot (different data, same
    // rationale: one fetch serves every user for CacheMinutes).
    private readonly SemaphoreSlim _cashSummaryLock = new(1, 1);
    private XeroCashSummarySnapshot? _cachedCashSummary;
    private DateTimeOffset _cachedCashSummaryAt = DateTimeOffset.MinValue;

    // Aged payables cache — separate from the transactions snapshot (that one is windowed by
    // FromDate and carries line items; this one is outstanding-only, unwindowed and line-free).
    private readonly SemaphoreSlim _agedPayablesLock = new(1, 1);
    private XeroAgedPayablesSnapshot? _cachedAgedPayables;
    private DateTimeOffset _cachedAgedPayablesAt = DateTimeOffset.MinValue;

    // Aged receivables cache — the sales-side mirror of the payables cache above.
    private readonly SemaphoreSlim _agedReceivablesLock = new(1, 1);
    private XeroAgedReceivablesSnapshot? _cachedAgedReceivables;
    private DateTimeOffset _cachedAgedReceivablesAt = DateTimeOffset.MinValue;

    // Supplier list cache — same rationale as the snapshots above (the contact list costs several
    // paged calls against the 60/min limit, and one fetch serves every user for CacheMinutes).
    private readonly SemaphoreSlim _suppliersLock = new(1, 1);
    private XeroSuppliersSnapshot? _cachedSuppliers;
    private DateTimeOffset _cachedSuppliersAt = DateTimeOffset.MinValue;

    // Tracking-categories snapshot cache — the Cost codes page's Xero tabs. One cheap call,
    // but it counts against the same 60/min budget as everything else (the write-back reads
    // the same endpoint), so a page of users mustn't each cost a call.
    private readonly SemaphoreSlim _trackingCategoriesLock = new(1, 1);
    private XeroTrackingCategoriesSnapshot? _cachedTrackingCategories;
    private DateTimeOffset _cachedTrackingCategoriesAt = DateTimeOffset.MinValue;

    // Chart of accounts changes rarely; refresh it hourly at most.
    private IReadOnlyDictionary<string, string>? _accountNamesByCode;
    private DateTimeOffset _accountNamesFetchedAt = DateTimeOffset.MinValue;

    // Tracking categories change rarely too, and every site-P&L read and write-back needs
    // them — cached briefly so a sync over N projects costs ONE call, not N (uncached, that
    // alone can spend Xero's 60/min budget and everything after it 429s). Distinct from the
    // Cost codes page's snapshot cache above: this is the write-back/P&L lookup shape.
    private TrackingCategoryLookup? _trackingLookup;
    private DateTimeOffset _trackingLookupAt = DateTimeOffset.MinValue;

    public XeroClient(HttpClient http, XeroOptions options, ILogger<XeroClient> logger)
    {
        _http = http;
        _options = options;
        _logger = logger;
    }

    public bool IsConfigured => _options.IsConfigured;

}

using System.Globalization;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

// ============================================================================
// XeroValuationBackfill — generates the historic valuation-invoice backfill SQL.
//
// WHY. Projects completed before JPMS went live carry their allocated Xero costs but no
// valuation invoices, so the Profit Summary shows "no certifications against cost" and a
// wholly negative profit for finished work. The certified rung of the ladder (claimed →
// certified → invoiced → paid) reads issued/paid valuation invoices — so the fix is to
// bring the historic sales invoices in as paid manual valuation invoices, exactly what
// CreateValuationInvoiceHandler records for an IsManual entry: backdated Issued/Paid,
// AmountPaid rolled into Projects.ValuationInvoicePaidTotal, and a ManualEntry audit event.
//
// WHAT IT DOES. Pulls every ACCREC sales invoice and ACCRECCREDIT credit note from Xero
// (custom connection, client-credentials grant — same auth as the api's XeroClient), keeps
// AUTHORISED and PAID ones, nets VAT off per line, groups them by the "Sites" tracking
// option on their lines, and emits one guarded SQL block per site. Credit notes come
// through as negative invoices so the certified total nets off correctly.
//
// WHAT IT DOES NOT DO. It never touches the database: the output is a script to review and
// run via sqlcmd (infra/run-backfill-valuation-invoices.sh). And the script itself only
// touches projects that are safe to backfill — each block resolves its project by the
// same XeroSiteName / project-name match the cost-allocation suggester uses, and SKIPs
// (with a PRINT) any project that already has valuation invoices or has a Preapproved
// claim (whose frozen totals a raw insert could not re-freeze — enter those through the
// app instead). Because of the already-has-invoices guard the script is idempotent: a
// re-run skips everything the first run inserted, and live projects like Woodhouse that
// already invoice through JPMS are never touched.
//
// USAGE.
//   dotnet run --project tools/XeroValuationBackfill [-- options]
//     --from 2022-08-01           earliest invoice date to read (default 2022-08-01)
//     --out <path>                output SQL path (default scripts/backfill-valuation-invoices.sql)
//     --client-id / --client-secret / --tenant-id
//                                 Xero custom-connection credentials; fall back to
//                                 XERO_CLIENT_ID / XERO_CLIENT_SECRET / XERO_TENANT_ID env vars,
//                                 then api/local.settings.json (Xero__ClientId / Xero__ClientSecret)
//     --map INV-0015=SomeSite     assign an invoice with no Sites tracking to a site (repeatable)
//     --exclude-site "By France"  leave a site out of the script entirely (repeatable)
// ============================================================================

var options = ToolOptions.Parse(args);
if (options is null) return 1;

Console.WriteLine($"Reading Xero sales invoices from {options.FromDate:yyyy-MM-dd}...");

var xero = new XeroSalesReader(options.ClientId!, options.ClientSecret!, options.TenantId);
List<SalesDocument> documents;
try
{
    documents = await xero.ReadSalesDocumentsAsync(options.FromDate);
}
catch (Exception failure)
{
    Console.Error.WriteLine($"Xero read failed: {failure.Message}");
    return 1;
}

Console.WriteLine($"  {documents.Count} sales documents fetched.");

var plan = BackfillPlan.Build(documents, options);
plan.WriteConsoleReport();

var sql = plan.ToSql(options);
Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(options.OutPath))!);
File.WriteAllText(options.OutPath, sql, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
Console.WriteLine();
Console.WriteLine($"SQL written to {options.OutPath}.");
Console.WriteLine("Review it, then apply with:  bash infra/run-backfill-valuation-invoices.sh");
return 0;

// ============================================================================

internal sealed record ToolOptions(
    string? ClientId,
    string? ClientSecret,
    string? TenantId,
    DateTime FromDate,
    string OutPath,
    IReadOnlyDictionary<string, string> SiteOverridesByInvoiceNumber,
    IReadOnlySet<string> ExcludedSites)
{
    public static ToolOptions? Parse(string[] args)
    {
        string? clientId = null, clientSecret = null, tenantId = null;
        var fromDate = new DateTime(2022, 8, 1);
        var outPath = Path.Combine("scripts", "backfill-valuation-invoices.sql");
        var overrides = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var excluded = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        for (var i = 0; i < args.Length; i++)
        {
            string Next(string flag) =>
                i + 1 < args.Length ? args[++i] : throw new ArgumentException($"{flag} needs a value.");
            try
            {
                switch (args[i])
                {
                    case "--client-id": clientId = Next("--client-id"); break;
                    case "--client-secret": clientSecret = Next("--client-secret"); break;
                    case "--tenant-id": tenantId = Next("--tenant-id"); break;
                    case "--from": fromDate = DateTime.Parse(Next("--from"), CultureInfo.InvariantCulture); break;
                    case "--out": outPath = Next("--out"); break;
                    case "--map":
                    {
                        var parts = Next("--map").Split('=', 2);
                        if (parts.Length != 2) { Console.Error.WriteLine("--map expects INV-0001=Site Name."); return null; }
                        overrides[parts[0].Trim()] = parts[1].Trim();
                        break;
                    }
                    case "--exclude-site": excluded.Add(SiteKey.Normalise(Next("--exclude-site"))); break;
                    default: Console.Error.WriteLine($"Unknown argument {args[i]}."); return null;
                }
            }
            catch (Exception parseFailure)
            {
                Console.Error.WriteLine(parseFailure.Message);
                return null;
            }
        }

        clientId ??= Environment.GetEnvironmentVariable("XERO_CLIENT_ID");
        clientSecret ??= Environment.GetEnvironmentVariable("XERO_CLIENT_SECRET");
        tenantId ??= Environment.GetEnvironmentVariable("XERO_TENANT_ID");

        if (clientId is null || clientSecret is null)
        {
            var settings = LocalSettings.TryRead();
            clientId ??= settings?.GetValueOrDefault("Xero__ClientId");
            clientSecret ??= settings?.GetValueOrDefault("Xero__ClientSecret");
            tenantId ??= settings?.GetValueOrDefault("Xero__TenantId");
        }

        if (string.IsNullOrWhiteSpace(clientId) || string.IsNullOrWhiteSpace(clientSecret))
        {
            Console.Error.WriteLine(
                "No Xero credentials. Pass --client-id/--client-secret, set XERO_CLIENT_ID/XERO_CLIENT_SECRET, " +
                "or make sure api/local.settings.json carries Xero__ClientId / Xero__ClientSecret.");
            return null;
        }

        return new ToolOptions(clientId, clientSecret, tenantId, fromDate, outPath, overrides, excluded);
    }
}

/// <summary>Reads Xero__* values out of api/local.settings.json (Azure Functions local format).</summary>
internal static class LocalSettings
{
    public static Dictionary<string, string>? TryRead()
    {
        var directory = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (directory is not null)
        {
            var candidate = Path.Combine(directory.FullName, "api", "local.settings.json");
            if (File.Exists(candidate))
            {
                using var doc = JsonDocument.Parse(File.ReadAllText(candidate));
                if (doc.RootElement.TryGetProperty("Values", out var values) && values.ValueKind == JsonValueKind.Object)
                    return values.EnumerateObject()
                        .Where(p => p.Value.ValueKind == JsonValueKind.String)
                        .ToDictionary(p => p.Name, p => p.Value.GetString()!, StringComparer.OrdinalIgnoreCase);
                return null;
            }
            directory = directory.Parent;
        }
        return null;
    }
}

/// <summary>The same normalisation XeroAllocationSuggester and the generated SQL use: case- and space-insensitive.</summary>
internal static class SiteKey
{
    public static string Normalise(string value) => value.Replace(" ", "").ToLowerInvariant();
}

/// <summary>One ACCREC invoice or ACCRECCREDIT credit note, with per-site net amounts off its lines.</summary>
internal sealed record SalesDocument(
    string XeroId,
    bool IsCreditNote,
    string? Number,
    string? Reference,
    string? ContactName,
    DateTime Date,
    string Status,          // AUTHORISED | PAID (others are filtered out at read time)
    decimal SubTotal,       // net of VAT; negative for credit notes
    decimal AmountDue,      // outstanding (credit notes: remaining credit), gross
    DateTime? FullyPaidOn,
    IReadOnlyDictionary<string, decimal> NetBySite,  // keyed by the Sites tracking option, verbatim
    decimal NetWithoutSite)                          // lines carrying no Sites tracking
{
    public bool IsPaid => Status == "PAID";
}

/// <summary>
/// Minimal ACCREC reader over a Xero custom connection — deliberately self-contained (this is a
/// standalone one-off tool) but shaped like the api's XeroClient: paged /Invoices and /CreditNotes
/// reads (paged responses include line items, which carry the Sites tracking), VAT netted off
/// inclusive lines, dates read from the *String fields with the /Date(ms)/ format as fallback.
/// </summary>
internal sealed class XeroSalesReader(string clientId, string clientSecret, string? tenantId)
{
    private const string TokenUrl = "https://identity.xero.com/connect/token";
    private const string InvoicesUrl = "https://api.xero.com/api.xro/2.0/Invoices";
    private const string CreditNotesUrl = "https://api.xero.com/api.xro/2.0/CreditNotes";
    private const int PageSize = 100;
    private const int MaxPages = 100;

    private readonly HttpClient http = new();

    public async Task<List<SalesDocument>> ReadSalesDocumentsAsync(DateTime from)
    {
        var token = await GetAccessTokenAsync();
        var documents = new List<SalesDocument>();
        await ReadAllPagesAsync(token, InvoicesUrl, "Invoices", "ACCREC", from, isCreditNote: false, documents);
        await ReadAllPagesAsync(token, CreditNotesUrl, "CreditNotes", "ACCRECCREDIT", from, isCreditNote: true, documents);
        return documents;
    }

    private async Task GetPageDelayAsync() => await Task.Delay(TimeSpan.FromMilliseconds(1100)); // Xero: 60 calls/min.

    private async Task ReadAllPagesAsync(
        string token, string baseUrl, string collection, string xeroType, DateTime from,
        bool isCreditNote, List<SalesDocument> into)
    {
        for (var page = 1; page <= MaxPages; page++)
        {
            var where = $"Type==\"{xeroType}\" AND Date >= DateTime({from.Year},{from.Month:D2},{from.Day:D2})";
            var url = $"{baseUrl}?page={page}&where={Uri.EscapeDataString(where)}&order={Uri.EscapeDataString("Date ASC")}";

            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            if (!string.IsNullOrWhiteSpace(tenantId)) request.Headers.Add("xero-tenant-id", tenantId);

            using var response = await http.SendAsync(request);
            var body = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode)
                throw new InvalidOperationException($"Xero rejected the {collection} page {page} read with HTTP {(int)response.StatusCode}: {Truncate(body)}");

            using var doc = JsonDocument.Parse(body);
            if (!doc.RootElement.TryGetProperty(collection, out var items) || items.ValueKind != JsonValueKind.Array)
                return;

            var pageCount = 0;
            foreach (var item in items.EnumerateArray())
            {
                pageCount++;
                var status = StringOf(item, "Status") ?? "UNKNOWN";
                if (status is not ("AUTHORISED" or "PAID")) continue; // Draft/submitted/voided/deleted never certify.
                into.Add(ReadDocument(item, isCreditNote));
            }

            if (pageCount < PageSize) return; // Short page — no more to fetch.
            await GetPageDelayAsync();
        }
    }

    private static SalesDocument ReadDocument(JsonElement item, bool isCreditNote)
    {
        var subTotal = DecimalOf(item, "SubTotal");
        var vatInclusive = string.Equals(StringOf(item, "LineAmountTypes"), "Inclusive", StringComparison.OrdinalIgnoreCase);

        var netBySite = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
        var netWithoutSite = 0m;
        if (item.TryGetProperty("LineItems", out var lines) && lines.ValueKind == JsonValueKind.Array)
        {
            foreach (var line in lines.EnumerateArray())
            {
                var lineAmount = DecimalOf(line, "LineAmount");
                if (vatInclusive) lineAmount -= DecimalOf(line, "TaxAmount");
                var site = SiteOf(line);
                if (site is null) netWithoutSite += lineAmount;
                else netBySite[site] = netBySite.GetValueOrDefault(site) + lineAmount;
            }
        }
        else
        {
            netWithoutSite = subTotal;
        }

        var sign = isCreditNote ? -1m : 1m;

        return new SalesDocument(
            XeroId: StringOf(item, "InvoiceID") ?? StringOf(item, "CreditNoteID") ?? Guid.NewGuid().ToString(),
            IsCreditNote: isCreditNote,
            Number: StringOf(item, "InvoiceNumber") ?? StringOf(item, "CreditNoteNumber"),
            Reference: StringOf(item, "Reference"),
            ContactName: item.TryGetProperty("Contact", out var contact) ? StringOf(contact, "Name") : null,
            Date: DateOf(item, "DateString", "Date") ?? DateTime.MinValue,
            Status: StringOf(item, "Status")!,
            SubTotal: sign * subTotal,
            AmountDue: item.TryGetProperty("AmountDue", out _) ? DecimalOf(item, "AmountDue") : DecimalOf(item, "RemainingCredit"),
            FullyPaidOn: DateOf(item, "FullyPaidOnDateString", "FullyPaidOnDate"),
            NetBySite: netBySite.ToDictionary(entry => entry.Key, entry => sign * entry.Value, StringComparer.OrdinalIgnoreCase),
            NetWithoutSite: sign * netWithoutSite);
    }

    private static string? SiteOf(JsonElement line)
    {
        if (!line.TryGetProperty("Tracking", out var tracking) || tracking.ValueKind != JsonValueKind.Array)
            return null;
        foreach (var entry in tracking.EnumerateArray())
        {
            var name = StringOf(entry, "Name");
            if (name is not null && SiteKey.Normalise(name) == "sites")
                return StringOf(entry, "Option");
        }
        return null;
    }

    private async Task<string> GetAccessTokenAsync()
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, TokenUrl);
        request.Headers.Authorization = new AuthenticationHeaderValue(
            "Basic", Convert.ToBase64String(Encoding.UTF8.GetBytes($"{clientId}:{clientSecret}")));
        // No scope parameter: the custom connection grants everything it was set up with (see XeroOptions).
        request.Content = new FormUrlEncodedContent(new Dictionary<string, string> { ["grant_type"] = "client_credentials" });

        using var response = await http.SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"Xero token request failed with HTTP {(int)response.StatusCode}: {Truncate(body)}");

        using var doc = JsonDocument.Parse(body);
        return doc.RootElement.GetProperty("access_token").GetString()
               ?? throw new InvalidOperationException("Xero token response carried no access_token.");
    }

    private static string? StringOf(JsonElement element, string property) =>
        element.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;

    private static decimal DecimalOf(JsonElement element, string property) =>
        element.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.Number ? value.GetDecimal() : 0m;

    private static DateTime? DateOf(JsonElement element, string stringProperty, string msProperty)
    {
        var text = StringOf(element, stringProperty);
        if (text is not null && DateTime.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
            return parsed;

        // Fallback: Xero's legacy "/Date(1700000000000+0000)/" form.
        var raw = StringOf(element, msProperty);
        if (raw is null) return null;
        var match = Regex.Match(raw, @"/Date\((\-?\d+)");
        return match.Success && long.TryParse(match.Groups[1].Value, out var ms)
            ? DateTimeOffset.FromUnixTimeMilliseconds(ms).UtcDateTime
            : null;
    }

    private static string Truncate(string body) => body.Length <= 300 ? body : body[..300] + "…";
}

/// <summary>One valuation invoice the SQL will insert: a document (or a document's share of one site).</summary>
internal sealed record PlannedInvoice(
    string ValuationInvoiceId,
    string EventId,
    SalesDocument Document,
    decimal Net,
    bool SplitAcrossSites);

/// <summary>Everything the run decided: per-site inserts, plus what was left out and why.</summary>
internal sealed class BackfillPlan
{
    public required SortedDictionary<string, List<PlannedInvoice>> InvoicesBySite { get; init; }
    public required List<SalesDocument> Unmatched { get; init; }          // no Sites tracking, no --map
    public required List<(SalesDocument Document, string Site)> Excluded { get; init; }

    public static BackfillPlan Build(IEnumerable<SalesDocument> documents, ToolOptions options)
    {
        var bySite = new SortedDictionary<string, List<PlannedInvoice>>(StringComparer.OrdinalIgnoreCase);
        var unmatched = new List<SalesDocument>();
        var excluded = new List<(SalesDocument, string)>();

        void Add(string site, SalesDocument document, decimal net, bool split)
        {
            if (Math.Round(net, 2) == 0m) return;
            if (options.ExcludedSites.Contains(SiteKey.Normalise(site))) { excluded.Add((document, site)); return; }
            if (!bySite.TryGetValue(site, out var list)) bySite[site] = list = new List<PlannedInvoice>();
            list.Add(new PlannedInvoice(NewId(), NewId(), document, Math.Round(net, 2), split));
        }

        foreach (var document in documents)
        {
            var sites = document.NetBySite.Where(entry => Math.Round(entry.Value, 2) != 0m).ToList();
            var siteless = document.NetWithoutSite;

            if (sites.Count == 0)
            {
                // Whole document has no Sites tracking — a --map override can still place it.
                if (document.Number is not null
                    && options.SiteOverridesByInvoiceNumber.TryGetValue(document.Number, out var mappedSite))
                    Add(mappedSite, document, document.SubTotal, split: false);
                else if (Math.Round(document.SubTotal, 2) != 0m)
                    unmatched.Add(document);
                continue;
            }

            if (sites.Count == 1)
            {
                // The common case: one site. Any siteless remainder (delivery lines, rounding)
                // belongs with it — use the invoice's own net so the certified total matches Xero.
                Add(sites[0].Key, document, sites[0].Value + siteless, split: false);
                continue;
            }

            // Lines spread across sites: one invoice per site share. A siteless remainder has no
            // home — flag the document so the review can place the difference by hand.
            foreach (var (site, net) in sites)
                Add(site, document, net, split: true);
            if (Math.Round(siteless, 2) != 0m)
                unmatched.Add(document with { SubTotal = siteless, NetBySite = new Dictionary<string, decimal>() });
        }

        foreach (var list in bySite.Values)
            list.Sort((a, b) => a.Document.Date != b.Document.Date
                ? a.Document.Date.CompareTo(b.Document.Date)
                : string.CompareOrdinal(a.Document.Number, b.Document.Number));

        return new BackfillPlan { InvoicesBySite = bySite, Unmatched = unmatched, Excluded = excluded };
    }

    public void WriteConsoleReport()
    {
        Console.WriteLine();
        Console.WriteLine($"{"Site",-40} {"Invoices",8} {"Net",15} {"Paid",15}");
        foreach (var (site, invoices) in InvoicesBySite)
            Console.WriteLine($"{site,-40} {invoices.Count,8} {invoices.Sum(entry => entry.Net),15:N2} {invoices.Where(entry => entry.Document.IsPaid).Sum(entry => entry.Net),15:N2}");
        Console.WriteLine($"{"TOTAL",-40} {InvoicesBySite.Values.Sum(list => list.Count),8} {InvoicesBySite.Values.Sum(list => list.Sum(entry => entry.Net)),15:N2}");

        if (Excluded.Count > 0)
        {
            Console.WriteLine();
            Console.WriteLine($"Excluded by --exclude-site: {Excluded.Count} documents.");
        }
        if (Unmatched.Count > 0)
        {
            Console.WriteLine();
            Console.WriteLine("No Sites tracking (not in the script — add --map INV-XXXX=\"Site Name\" to place one):");
            foreach (var document in Unmatched)
                Console.WriteLine($"  {document.Number,-12} {document.Date:yyyy-MM-dd}  {document.SubTotal,12:N2}  {document.ContactName} — {document.Reference}");
        }
    }

    public string ToSql(ToolOptions options)
    {
        var sql = new StringBuilder();
        sql.AppendLine("-- ============================================================================");
        sql.AppendLine("-- Historic valuation-invoice backfill — GENERATED by tools/XeroValuationBackfill.");
        sql.AppendLine($"-- Source: Xero ACCREC invoices + ACCRECCREDIT credit notes from {options.FromDate:yyyy-MM-dd},");
        sql.AppendLine("-- statuses AUTHORISED/PAID, grouped by the \"Sites\" tracking option, VAT netted off.");
        sql.AppendLine("--");
        sql.AppendLine("-- Each site is one guarded, transactional batch that:");
        sql.AppendLine("--   * resolves the project by XeroSiteName (or project name), case/space-insensitive —");
        sql.AppendLine("--     the same match the Xero cost-allocation suggester uses;");
        sql.AppendLine("--   * SKIPs (PRINT, no error) projects with ANY existing valuation invoice — this is what");
        sql.AppendLine("--     makes the script idempotent and keeps it away from live projects that already");
        sql.AppendLine("--     invoice through JPMS;");
        sql.AppendLine("--   * SKIPs projects holding a Preapproved claim (a raw insert cannot re-freeze its");
        sql.AppendLine("--     totals — enter those through the app's manual-invoice flow instead);");
        sql.AppendLine("--   * inserts each sale as a paid/issued MANUAL valuation invoice (IsManual = 1,");
        sql.AppendLine("--     backdated, no report snapshot — mirroring CreateValuationInvoiceHandler), with a");
        sql.AppendLine("--     ManualEntry audit event naming the Xero invoice;");
        sql.AppendLine("--   * rolls the paid total into Projects.ValuationInvoicePaidTotal.");
        sql.AppendLine("--");
        sql.AppendLine("-- Apply with:  bash infra/run-backfill-valuation-invoices.sh   (sqlcmd -b; read the log)");
        sql.AppendLine("-- ============================================================================");

        if (Unmatched.Count > 0)
        {
            sql.AppendLine("--");
            sql.AppendLine("-- NOT INCLUDED — no Sites tracking in Xero (re-run with --map INV-XXXX=\"Site Name\"):");
            foreach (var document in Unmatched)
                sql.AppendLine($"--   {document.Number,-12} {document.Date:yyyy-MM-dd} {document.SubTotal,12:N2}  {document.ContactName} — {document.Reference}");
        }

        foreach (var (site, invoices) in InvoicesBySite)
        {
            var net = invoices.Sum(entry => entry.Net);
            var paid = invoices.Where(entry => entry.Document.IsPaid).Sum(entry => entry.Net);
            var normalised = SiteKey.Normalise(site).Replace("'", "''");
            var label = site.Replace("'", "''");

            sql.AppendLine();
            sql.AppendLine("GO");
            sql.AppendLine($"-- ===== {site} — {invoices.Count} invoices, net {net:N2}, of which paid {paid:N2} =====");
            sql.AppendLine("SET XACT_ABORT ON;");
            sql.AppendLine("BEGIN TRAN;");
            sql.AppendLine("DECLARE @ProjectId nvarchar(64) = (");
            sql.AppendLine("    SELECT TOP 1 ProjectId FROM Projects");
            sql.AppendLine($"    WHERE LOWER(REPLACE(COALESCE(XeroSiteName, ''), ' ', '')) = '{normalised}'");
            sql.AppendLine($"       OR LOWER(REPLACE(Name, ' ', '')) = '{normalised}'");
            sql.AppendLine("    ORDER BY CASE WHEN LOWER(REPLACE(COALESCE(XeroSiteName, ''), ' ', '')) = '" + normalised + "' THEN 0 ELSE 1 END);");
            sql.AppendLine("IF @ProjectId IS NULL");
            sql.AppendLine($"    PRINT 'SKIP  {label} — no project matches this Xero site.';");
            sql.AppendLine("ELSE IF EXISTS (SELECT 1 FROM ValuationInvoices WHERE ProjectId = @ProjectId)");
            sql.AppendLine($"    PRINT 'SKIP  {label} — project already has valuation invoices; nothing touched.';");
            sql.AppendLine("ELSE IF EXISTS (SELECT 1 FROM ValuationClaims WHERE ProjectId = @ProjectId AND Status = 1)");
            sql.AppendLine($"    PRINT 'SKIP  {label} — project holds a Preapproved claim; use the app''s manual-invoice flow.';");
            sql.AppendLine("ELSE");
            sql.AppendLine("BEGIN");

            sql.AppendLine("    INSERT INTO ValuationInvoices");
            sql.AppendLine("        (ValuationInvoiceId, ProjectId, ValuationClaimId, Number, Reference, PeriodMonth,");
            sql.AppendLine("         Amount, AmountPaid, Status, RaisedAt, IssuedAt, PaidAt, AmendmentCount, IsManual)");
            sql.AppendLine("    VALUES");
            for (var index = 0; index < invoices.Count; index++)
            {
                var planned = invoices[index];
                var document = planned.Document;
                var number = index + 1;
                var period = new DateTime(document.Date.Year, document.Date.Month, 1);
                var issuedAt = SqlDate(document.Date);
                // Status 2 = Paid, 1 = Issued (ValuationInvoiceStatus). Paid documents record their
                // full net as paid; a still-open AUTHORISED one lands as Issued with nothing paid.
                var status = document.IsPaid ? 2 : 1;
                var amountPaid = document.IsPaid ? planned.Net : 0m;
                var paidAt = document.IsPaid ? SqlDate(document.FullyPaidOn ?? document.Date) : "NULL";
                var comma = index < invoices.Count - 1 ? "," : ";";
                sql.AppendLine(
                    $"        ('{planned.ValuationInvoiceId}', @ProjectId, NULL, {number}, 'VI-{number:0000}', {SqlDate(period)}," +
                    $" {SqlMoney(planned.Net)}, {SqlMoney(amountPaid)}, {status}, {issuedAt}, {issuedAt}, {paidAt}, 0, 1){comma}");
            }

            sql.AppendLine("    INSERT INTO ValuationInvoiceEvents");
            sql.AppendLine("        (ValuationInvoiceEventId, ValuationInvoiceId, EventType, OccurredAt, Note, AmountAfter)");
            sql.AppendLine("    VALUES");
            for (var index = 0; index < invoices.Count; index++)
            {
                var planned = invoices[index];
                var comma = index < invoices.Count - 1 ? "," : ";";
                // EventType 8 = ManualEntry. The note carries the Xero identity — the only place it lives.
                sql.AppendLine(
                    $"        ('{planned.EventId}', '{planned.ValuationInvoiceId}', 8, {SqlDate(planned.Document.Date)}," +
                    $" '{SqlText(NoteFor(planned))}', {SqlMoney(planned.Net)}){comma}");
            }

            sql.AppendLine($"    UPDATE Projects SET ValuationInvoicePaidTotal = ValuationInvoicePaidTotal + {SqlMoney(paid)} WHERE ProjectId = @ProjectId;");
            sql.AppendLine($"    PRINT 'OK    {label} — {invoices.Count} invoices backfilled, net {net:N2} (paid {paid:N2}).';");
            sql.AppendLine("END");
            sql.AppendLine("COMMIT;");
        }

        sql.AppendLine();
        sql.AppendLine("GO");
        sql.AppendLine("-- Sanity check: certified (issued+paid) per backfilled project, A-Z.");
        sql.AppendLine("SELECT p.Name, COUNT(*) AS Invoices, SUM(vi.Amount) AS Certified, SUM(vi.AmountPaid) AS Paid");
        sql.AppendLine("FROM ValuationInvoices vi JOIN Projects p ON p.ProjectId = vi.ProjectId");
        sql.AppendLine("WHERE vi.IsManual = 1 GROUP BY p.Name ORDER BY p.Name;");
        return sql.ToString();
    }

    private static string NoteFor(PlannedInvoice planned)
    {
        var document = planned.Document;
        var kind = document.IsCreditNote ? "credit note" : "invoice";
        var share = planned.SplitAcrossSites ? " (this site's share of a multi-site invoice)" : "";
        var reference = string.IsNullOrWhiteSpace(document.Reference) ? "" : $" — {document.Reference}";
        return $"Backfilled from Xero {kind} {document.Number}{reference}{share}. Historic completed works (accounts export, Aug 2026).";
    }

    private static string SqlDate(DateTime date) => $"'{date:yyyy-MM-dd}T00:00:00+00:00'";
    private static string SqlMoney(decimal value) => value.ToString("0.00", CultureInfo.InvariantCulture);
    private static string SqlText(string value) => value.Replace("'", "''");
    private static string NewId() => Guid.NewGuid().ToString("N");
}

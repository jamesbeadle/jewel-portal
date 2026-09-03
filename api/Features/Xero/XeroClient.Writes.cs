using System.Net.Http.Headers;
using System.Text;
using System.Text.Json.Nodes;
using Jewel.JPMS.Contracts.Xero;

namespace Jewel.JPMS.Api.Features.Xero;

public sealed partial class XeroClient
{
    public async Task<XeroApprovalResult> ApproveInvoiceAsync(XeroApprovalRequest request, CancellationToken ct)
    {
        if (!_options.IsConfigured)
            return XeroApprovalResult.Failed(
                "Xero isn't connected — add the Xero__ClientId / Xero__ClientSecret app settings.");

        try
        {
            var token = await GetAccessTokenAsync(ct);

            var baseUrl = request.IsCreditNote ? CreditNotesUrl : InvoicesUrl;
            var collection = request.IsCreditNote ? "CreditNotes" : "Invoices";
            var idProperty = request.IsCreditNote ? "CreditNoteID" : "InvoiceID";

            // Always work from a fresh read — the stored ledger line may be minutes or
            // days old, and the update below replaces the invoice's entire line list.
            using var doc = await GetJsonAsync(token, $"{baseUrl}/{request.InvoiceId}", collection.ToLowerInvariant(), ct);
            if (!doc.RootElement.TryGetProperty(collection, out var items)
                || items.ValueKind != JsonValueKind.Array || items.GetArrayLength() == 0)
                return XeroApprovalResult.Failed("Xero returned no invoice for this id — it may have been deleted.");

            var invoice = items[0];
            var status = StringOf(invoice, "Status") ?? "UNKNOWN";
            if (status.Equals("AUTHORISED", StringComparison.OrdinalIgnoreCase)
                || status.Equals("PAID", StringComparison.OrdinalIgnoreCase))
                return XeroApprovalResult.SkippedAlreadyApproved(status);
            if (!status.Equals("DRAFT", StringComparison.OrdinalIgnoreCase)
                && !status.Equals("SUBMITTED", StringComparison.OrdinalIgnoreCase))
                return XeroApprovalResult.Failed($"The invoice is {status} in Xero and can't be approved.");

            var categories = await GetTrackingCategoriesAsync(token, ct);

            // Sites are an explicit per-project mapping — a missing option means the
            // mapping is wrong (or the option was renamed in Xero), so fail loudly.
            var missingSites = request.Lines.SelectMany(line => line.Shares)
                .Select(share => share.SiteOption)
                .Where(site => !categories.SiteOptions.Contains(site))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            if (missingSites.Count > 0)
                return XeroApprovalResult.Failed(
                    $"Xero's \"{_options.SiteTrackingCategory}\" tracking category has no option named "
                    + string.Join(", ", missingSites.Select(site => $"\"{site}\""))
                    + " — check the project's Xero site mapping against Xero's tracking options.");

            // Build (and thereby validate against drift) BEFORE touching Xero — creating
            // tracking options for an approval that then fails would mutate Xero for nothing.
            var lineItems = BuildLineItems(invoice, request, categories, out var buildError);
            if (lineItems is null)
                return XeroApprovalResult.Failed(buildError!);

            // Master cost codes are JPMS-owned; create any that Xero doesn't hold yet so
            // the confirmation can be recorded. (Xero caps a category at 100 options — a
            // rejection here surfaces verbatim for the finance team to resolve.)
            var missingCodes = request.Lines.SelectMany(line => line.Shares)
                .Select(share => share.CostCenterCode)
                .Where(code => !categories.CostCodeOptions.Contains(code))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            foreach (var code in missingCodes)
            {
                var optionBody = new JsonObject { ["Name"] = code };
                using var _ = await SendJsonAsync(HttpMethod.Put, token,
                    $"{TrackingCategoriesUrl}/{categories.CostCodeCategoryId}/Options",
                    optionBody, $"create Cost Code option {code}", ct);
                // The lookup is cached (see GetTrackingCategoriesAsync); record the option we
                // just created so an approval moments later doesn't try to create it again.
                categories.CostCodeOptions.Add(code);
            }

            var payload = new JsonObject
            {
                [idProperty] = request.InvoiceId,
                ["Status"] = "AUTHORISED",
                ["LineItems"] = lineItems
            };
            using var response = await SendJsonAsync(HttpMethod.Post, token,
                $"{baseUrl}/{request.InvoiceId}", payload, "approve invoice", ct);

            // The cached snapshot now lies about this invoice's status; drop it so the
            // next sync re-reads rather than resurrecting DRAFT for up to CacheMinutes.
            _cachedSnapshot = null;
            _cachedSnapshotAt = DateTimeOffset.MinValue;

            return XeroApprovalResult.Ok("AUTHORISED");
        }
        catch (XeroCallFailedException failure)
        {
            return XeroApprovalResult.Failed(failure.Message);
        }
    }

    // -- §6a settlement-schedule coding ------------------------------------------------------
    //
    // 2026-09-03 (the accountant's "coding run must settle a worker who already has a bill"):
    //   * RecodeBillAsync takes DRAFT, SUBMITTED and AUTHORISED bills — the cover route authorises
    //     the bill before the run sees it, so authorised is the NORMAL state, not an edge case —
    //     and refuses anything paid, credited, voided or deleted with the reason.
    //   * A recode PRESERVES the bill: status untouched, LineAmountTypes echoed, the existing tax
    //     type carried onto every new line, and the bill's own SubTotal / TotalTax / Total
    //     pro-rated across the schedule's weights penny-safe (XeroSplitMaths) — the schedule
    //     supplies the split, never the money. Adam's £3,200 No-VAT bill stays £3,200 No VAT.
    //   * A staged draft never assumes a tax type: the contact's default purchases tax type,
    //     else the tax type on the contact's most recent bill, else omitted (Xero's account
    //     default, which for CIS labour 321 is INPUT2 — the fault that produced £640 of VAT on
    //     a non-registered sole trader). The Note says which applied.

    /// <summary>
    /// Shared validation + tracking for the §6a writes: every site option must already exist in
    /// Xero (a missing one is a mapping fault, reported loudly), and missing cost-code options are
    /// created (JPMS owns the master list, same as approval). Returns the tracking lookup so the
    /// caller can build the lines.
    /// </summary>
    private async Task<(TrackingCategoryLookup? Categories, string? Error)> PrepareScheduleTrackingAsync(
        string token, IReadOnlyList<XeroScheduleLine> lines, CancellationToken ct)
    {
        var categories = await GetTrackingCategoriesAsync(token, ct);

        var missingSites = lines.Select(line => line.SiteOption)
            .Where(site => !categories.SiteOptions.Contains(site))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (missingSites.Count > 0)
            return (null,
                $"Xero's \"{_options.SiteTrackingCategory}\" tracking category has no option named "
                + string.Join(", ", missingSites.Select(site => $"\"{site}\""))
                + " — check the site's Xero mapping against Xero's tracking options.");

        var missingCodes = lines.Select(line => line.CostCodeOption)
            .Where(code => !categories.CostCodeOptions.Contains(code))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        foreach (var code in missingCodes)
        {
            var optionBody = new JsonObject { ["Name"] = code };
            using var _ = await SendJsonAsync(HttpMethod.Put, token,
                $"{TrackingCategoriesUrl}/{categories.CostCodeCategoryId}/Options",
                optionBody, $"create Cost Code option {code}", ct);
            categories.CostCodeOptions.Add(code);
        }
        return (categories, null);
    }

    /// <summary>One schedule line as a Xero line item: the given amounts, the tax type when known
    /// (omitted = Xero's account default), the schedule's tracking.</summary>
    private static JsonObject ScheduleLineItem(
        XeroScheduleLine line, decimal lineAmount, decimal? taxAmount, string? taxType, TrackingCategoryLookup categories)
    {
        var item = new JsonObject
        {
            ["Description"] = line.Description,
            ["Quantity"] = 1m,
            ["UnitAmount"] = lineAmount,
            ["LineAmount"] = lineAmount,
            ["AccountCode"] = line.AccountCode,
            ["Tracking"] = TrackingFor(categories, line.SiteOption, line.CostCodeOption)
        };
        if (taxAmount is not null) item["TaxAmount"] = taxAmount.Value;
        if (!string.IsNullOrWhiteSpace(taxType)) item["TaxType"] = taxType;
        return item;
    }

    public async Task<XeroBillSummary?> GetBillAsync(string invoiceId, CancellationToken ct)
    {
        if (!_options.IsConfigured)
            throw new XeroCallFailedException(
                "Xero isn't connected — add the Xero__ClientId / Xero__ClientSecret app settings.");
        var token = await GetAccessTokenAsync(ct);
        JsonDocument doc;
        try
        {
            doc = await GetJsonAsync(token, $"{InvoicesUrl}/{invoiceId}", "invoices", ct);
        }
        catch (XeroCallFailedException failure) when (failure.Message.Contains("HTTP 404"))
        {
            return null; // Xero has no bill by that id — deleted, or never existed.
        }
        using (doc)
        {
            if (!doc.RootElement.TryGetProperty("Invoices", out var items)
                || items.ValueKind != JsonValueKind.Array || items.GetArrayLength() == 0)
                return null;
            return ReadBillSummary(items[0]);
        }
    }

    private static XeroBillSummary ReadBillSummary(JsonElement invoice)
    {
        var lineCount = 0;
        string? taxType = null;
        if (invoice.TryGetProperty("LineItems", out var lines) && lines.ValueKind == JsonValueKind.Array)
        {
            lineCount = lines.GetArrayLength();
            taxType = DominantTaxType(lines);
        }
        return new XeroBillSummary(
            StringOf(invoice, "InvoiceID") ?? "",
            StringOf(invoice, "Status") ?? "UNKNOWN",
            StringOf(invoice, "InvoiceNumber"),
            StringOf(invoice, "Reference"),
            invoice.TryGetProperty("Contact", out var contact) ? StringOf(contact, "Name") : null,
            DateOf(invoice, "DateString", "Date"),
            StringOf(invoice, "LineAmountTypes") ?? "Exclusive",
            DecimalOf(invoice, "SubTotal"),
            DecimalOf(invoice, "TotalTax"),
            DecimalOf(invoice, "Total"),
            DecimalOf(invoice, "AmountPaid"),
            DecimalOf(invoice, "AmountCredited"),
            DecimalOf(invoice, "AmountDue"),
            lineCount,
            taxType);
    }

    /// <summary>The tax type the bill's lines carry — the one on the largest line when they
    /// differ (a Dext bill is one line, so this is simply its tax type). Null when none is set.</summary>
    private static string? DominantTaxType(JsonElement lines)
    {
        string? best = null;
        var bestAmount = decimal.MinValue;
        foreach (var line in lines.EnumerateArray())
        {
            var taxType = StringOf(line, "TaxType");
            if (string.IsNullOrWhiteSpace(taxType)) continue;
            var amount = Math.Abs(DecimalOf(line, "LineAmount"));
            if (amount > bestAmount) { best = taxType; bestAmount = amount; }
        }
        return best;
    }

    public async Task<XeroBillRecodeResult> RecodeBillAsync(XeroBillCodingRequest request, CancellationToken ct)
    {
        if (!_options.IsConfigured)
            return XeroBillRecodeResult.Failed(
                "Xero isn't connected — add the Xero__ClientId / Xero__ClientSecret app settings.");
        if (request.Lines.Count == 0)
            return XeroBillRecodeResult.Failed("Nothing to code — the schedule has no lines.");
        try
        {
            var token = await GetAccessTokenAsync(ct);

            // Always from a fresh read — the update below replaces the bill's entire line list,
            // and the decision (editable? what VAT? what total?) must be made on what Xero holds
            // NOW, not on a ledger row synced last night.
            using var doc = await GetJsonAsync(token, $"{InvoicesUrl}/{request.InvoiceId}", "invoices", ct);
            if (!doc.RootElement.TryGetProperty("Invoices", out var items)
                || items.ValueKind != JsonValueKind.Array || items.GetArrayLength() == 0)
                return XeroBillRecodeResult.Failed("Xero returned no bill for this id — it may have been deleted.");

            var invoice = items[0];
            var before = ReadBillSummary(invoice);
            if (!before.IsRecodable)
                return XeroBillRecodeResult.Failed(
                    $"Bill {before.InvoiceNumber ?? before.InvoiceId} can't be recoded — {before.NotRecodableReason}. "
                    + "Xero only allows line edits while nothing is paid or credited against a bill.");

            var (categories, error) = await PrepareScheduleTrackingAsync(token, request.Lines, ct);
            if (categories is null) return XeroBillRecodeResult.Failed(error!);

            // The schedule is the SPLIT; the bill is the MONEY. Pro-rate the bill's own figures
            // across the schedule's net weights: an Inclusive bill's LineAmount carries the VAT,
            // an Exclusive/NoTax bill's doesn't, and the TaxAmount is pro-rated alongside so
            // Xero's per-line recalculation can't drift the total by a penny.
            var inclusive = before.LineAmountTypes.Equals("Inclusive", StringComparison.OrdinalIgnoreCase);
            var weights = request.Lines.Select(line => line.Net).ToList();
            if (weights.Sum() == 0m)
                return XeroBillRecodeResult.Failed("The schedule's lines sum to £0.00 — nothing to split the bill across.");
            var amounts = XeroSplitMaths.ProportionalShares(inclusive ? before.Total : before.SubTotal, weights);
            var taxes = XeroSplitMaths.ProportionalShares(before.TotalTax, weights);

            var lineItems = new JsonArray();
            for (var i = 0; i < request.Lines.Count; i++)
                lineItems.Add(ScheduleLineItem(request.Lines[i], amounts[i], taxes[i], before.TaxType, categories));

            // No Status in the payload: the bill keeps whatever status it has — a draft stays
            // draft for the accountant, an authorised bill stays authorised. LineAmountTypes is
            // echoed so the amounts above mean what they meant.
            var payload = new JsonObject
            {
                ["InvoiceID"] = request.InvoiceId,
                ["LineAmountTypes"] = before.LineAmountTypes,
                ["LineItems"] = lineItems
            };
            using var response = await SendJsonAsync(HttpMethod.Post, token,
                $"{InvoicesUrl}/{request.InvoiceId}", payload, "recode bill", ct);

            _cachedSnapshot = null;
            _cachedSnapshotAt = DateTimeOffset.MinValue;

            // Xero answers with the bill as it now stands — the fresh LineItemIDs are what the
            // caller re-points the timesheet cover onto, and the totals are the proof the recode
            // moved nothing.
            var written = new List<XeroRecodedLine>();
            var after = before;
            if (response.RootElement.TryGetProperty("Invoices", out var updated)
                && updated.ValueKind == JsonValueKind.Array && updated.GetArrayLength() > 0)
            {
                after = ReadBillSummary(updated[0]);
                if (updated[0].TryGetProperty("LineItems", out var freshLines) && freshLines.ValueKind == JsonValueKind.Array)
                    foreach (var line in freshLines.EnumerateArray())
                        written.Add(new XeroRecodedLine(
                            StringOf(line, "LineItemID") ?? "",
                            StringOf(line, "Description") ?? "",
                            DecimalOf(line, "LineAmount"),
                            DecimalOf(line, "TaxAmount"),
                            StringOf(line, "AccountCode") ?? "",
                            TrackingOptionOf(line, _options.SiteTrackingCategory),
                            TrackingOptionOf(line, _options.CostCodeTrackingCategory)));
            }
            return new XeroBillRecodeResult(true, null, after.Status, after.LineAmountTypes, before.TaxType,
                after.SubTotal, after.TotalTax, after.Total, written);
        }
        catch (XeroCallFailedException failure)
        {
            return XeroBillRecodeResult.Failed(failure.Message);
        }
    }

    public async Task<XeroApprovalResult> CreateDraftBillAsync(XeroDraftBillRequest request, CancellationToken ct)
    {
        if (!_options.IsConfigured)
            return XeroApprovalResult.Failed(
                "Xero isn't connected — add the Xero__ClientId / Xero__ClientSecret app settings.");
        try
        {
            var token = await GetAccessTokenAsync(ct);

            var (categories, error) = await PrepareScheduleTrackingAsync(token, request.Lines, ct);
            if (categories is null) return XeroApprovalResult.Failed(error!);

            // The tax type is never assumed. Read the contact; take its default purchases tax
            // type; failing that, what they charged on their last bill; failing that, leave it
            // to Xero's account default and SAY so — the run relays the note.
            var (contactId, taxType, taxNote) = await ResolveContactTaxTypeAsync(token, request.ContactName, ct);

            var lineItems = new JsonArray();
            foreach (var line in request.Lines)
                lineItems.Add(ScheduleLineItem(line, line.Net, null, taxType, categories));

            var payload = new JsonObject
            {
                ["Type"] = "ACCPAY",
                ["Contact"] = contactId is null
                    ? new JsonObject { ["Name"] = request.ContactName }
                    : new JsonObject { ["ContactID"] = contactId },
                ["Date"] = request.Date.ToString("yyyy-MM-dd"),
                ["DueDate"] = request.DueDate.ToString("yyyy-MM-dd"),
                ["Reference"] = request.Reference,
                ["Status"] = "DRAFT",
                ["LineAmountTypes"] = "Exclusive",
                ["LineItems"] = lineItems
            };
            using var response = await SendJsonAsync(HttpMethod.Put, token, InvoicesUrl, payload, "stage draft bill", ct);

            var billId = "";
            var note = taxNote;
            if (response.RootElement.TryGetProperty("Invoices", out var created)
                && created.ValueKind == JsonValueKind.Array && created.GetArrayLength() > 0)
            {
                billId = StringOf(created[0], "InvoiceID") ?? "";
                var summary = ReadBillSummary(created[0]);
                note += $" Staged net £{summary.SubTotal:N2}, VAT £{summary.TotalTax:N2}, total £{summary.Total:N2}.";
            }

            _cachedSnapshot = null;
            _cachedSnapshotAt = DateTimeOffset.MinValue;
            return XeroApprovalResult.Ok(billId, note);
        }
        catch (XeroCallFailedException failure)
        {
            return XeroApprovalResult.Failed(failure.Message);
        }
    }

    /// <summary>
    /// The contact as Xero holds it (by exact name) and the tax type to stage their bill with:
    /// (ContactID or null, TaxType or null, a sentence saying where the tax type came from).
    /// A read failure here is not fatal to the staging — the bill still goes in, with the
    /// account default and a note that says the contact couldn't be read.
    /// </summary>
    private async Task<(string? ContactId, string? TaxType, string Note)> ResolveContactTaxTypeAsync(
        string token, string contactName, CancellationToken ct)
    {
        try
        {
            var escapedName = contactName.Replace("\\", "\\\\").Replace("\"", "\\\"");
            var contactsUrl = $"{ContactsUrl}?where={Uri.EscapeDataString($"Name==\"{escapedName}\"")}";
            using var contactsDoc = await GetJsonAsync(token, contactsUrl, "contacts", ct);
            if (!contactsDoc.RootElement.TryGetProperty("Contacts", out var contacts)
                || contacts.ValueKind != JsonValueKind.Array || contacts.GetArrayLength() == 0)
                return (null, null,
                    $"Xero has no contact named \"{contactName}\" — one was created with the bill, "
                    + "and Xero's account default tax type applied: check the VAT on the bill and set "
                    + "the contact's default purchases tax type.");

            var contact = contacts[0];
            var contactId = StringOf(contact, "ContactID");
            var contactDefault = StringOf(contact, "AccountsPayableTaxType");
            if (!string.IsNullOrWhiteSpace(contactDefault))
                return (contactId, contactDefault, $"Tax type {contactDefault} from the contact's default.");

            if (contactId is not null)
            {
                var billsUrl = $"{InvoicesUrl}?where={Uri.EscapeDataString($"Type==\"ACCPAY\"&&Contact.ContactID==Guid(\"{contactId}\")")}"
                    + $"&order={Uri.EscapeDataString("Date DESC")}&page=1";
                using var billsDoc = await GetJsonAsync(token, billsUrl, "invoices", ct);
                if (billsDoc.RootElement.TryGetProperty("Invoices", out var bills) && bills.ValueKind == JsonValueKind.Array)
                    foreach (var bill in bills.EnumerateArray())
                    {
                        var status = StringOf(bill, "Status") ?? "";
                        if (status.Equals("VOIDED", StringComparison.OrdinalIgnoreCase)
                            || status.Equals("DELETED", StringComparison.OrdinalIgnoreCase)) continue;
                        if (!bill.TryGetProperty("LineItems", out var lines) || lines.ValueKind != JsonValueKind.Array) continue;
                        var previous = DominantTaxType(lines);
                        if (previous is null) continue;
                        return (contactId, previous,
                            $"Tax type {previous} from the contact's most recent bill "
                            + $"({StringOf(bill, "InvoiceNumber") ?? StringOf(bill, "Reference") ?? "unnumbered"}, "
                            + $"{DateOf(bill, "DateString", "Date"):dd MMM yyyy}) — the contact has no default set.");
                    }
            }

            return (contactId, null,
                "The contact has no default purchases tax type and no previous bill — Xero's "
                + "account default applied: check the VAT on the bill and set the contact's default.");
        }
        catch (XeroCallFailedException failure)
        {
            return (null, null,
                $"Couldn't read the contact's tax type ({failure.Message}) — Xero's account default "
                + "applied: check the VAT on the bill.");
        }
    }

    // -- site-only tracking update (SetProject half-step, no approval) -----------------

    public async Task<XeroApprovalResult> SetSiteTrackingAsync(XeroSiteTrackingRequest request, CancellationToken ct)
    {
        if (!_options.IsConfigured)
            return XeroApprovalResult.Failed(
                "Xero isn't connected — add the Xero__ClientId / Xero__ClientSecret app settings.");

        try
        {
            var token = await GetAccessTokenAsync(ct);

            var baseUrl = request.IsCreditNote ? CreditNotesUrl : InvoicesUrl;
            var collection = request.IsCreditNote ? "CreditNotes" : "Invoices";
            var idProperty = request.IsCreditNote ? "CreditNoteID" : "InvoiceID";

            // Fresh read, same as approval: the update below replaces the whole line list.
            using var doc = await GetJsonAsync(token, $"{baseUrl}/{request.InvoiceId}", collection.ToLowerInvariant(), ct);
            if (!doc.RootElement.TryGetProperty(collection, out var items)
                || items.ValueKind != JsonValueKind.Array || items.GetArrayLength() == 0)
                return XeroApprovalResult.Failed("Xero returned no invoice for this id — it may have been deleted.");

            var invoice = items[0];
            var status = StringOf(invoice, "Status") ?? "UNKNOWN";
            // Approved-but-unpaid bills accept the tracking update (decision 2026-08-14:
            // a cost moved between projects after approval must follow through to Xero's
            // Sites tracking). Paid bills are locked — Xero refuses line edits once
            // payments are applied — so those are skipped as a silent success, same as
            // the approval path: the JPMS move stands, Xero keeps its record.
            if (status.Equals("PAID", StringComparison.OrdinalIgnoreCase))
                return XeroApprovalResult.SkippedAlreadyApproved(status);
            if (!status.Equals("DRAFT", StringComparison.OrdinalIgnoreCase)
                && !status.Equals("SUBMITTED", StringComparison.OrdinalIgnoreCase)
                && !status.Equals("AUTHORISED", StringComparison.OrdinalIgnoreCase))
                return XeroApprovalResult.Failed(
                    $"The invoice is {status} in Xero — its tracking can't be updated.");

            var categories = await GetTrackingCategoriesAsync(token, ct);

            var missingSites = request.Lines
                .Select(line => line.SiteOption)
                .Where(site => site is not null && !categories.SiteOptions.Contains(site))
                .Select(site => site!)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            if (missingSites.Count > 0)
                return XeroApprovalResult.Failed(
                    $"Xero's \"{_options.SiteTrackingCategory}\" tracking category has no option named "
                    + string.Join(", ", missingSites.Select(site => $"\"{site}\""))
                    + " — check the project's Xero site mapping against Xero's tracking options.");

            // Rebuild the full line list: every line passes through as-is except the
            // targets, whose Sites entry is replaced. No amount checks needed — nothing
            // about the money changes, and no lines are split.
            var sitesByLineId = request.Lines.ToDictionary(
                line => line.LineItemId, line => line.SiteOption, StringComparer.OrdinalIgnoreCase);
            var seenLineIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var lineItems = new JsonArray();
            if (invoice.TryGetProperty("LineItems", out var originals) && originals.ValueKind == JsonValueKind.Array)
            {
                foreach (var line in originals.EnumerateArray())
                {
                    var lineItemId = StringOf(line, "LineItemID");
                    if (lineItemId is not null && sitesByLineId.TryGetValue(lineItemId, out var siteOption))
                    {
                        seenLineIds.Add(lineItemId);
                        var copy = CopyLine(line, keepLineItemId: true, keepTracking: false, includeTaxAmount: true);
                        copy["Tracking"] = ReplaceSiteTracking(line, categories, siteOption);
                        lineItems.Add(copy);
                    }
                    else
                    {
                        lineItems.Add(CopyLine(line, keepLineItemId: true, keepTracking: true, includeTaxAmount: true));
                    }
                }
            }

            var unmatched = sitesByLineId.Keys.Where(id => !seenLineIds.Contains(id)).ToList();
            if (unmatched.Count > 0)
                return XeroApprovalResult.Failed(
                    "The bill's lines have changed in Xero since they were synced "
                    + $"({unmatched.Count} line(s) no longer exist). Sync from Xero and try again.");

            // No Status in the payload: the bill keeps whatever status it has (draft
            // bills stay draft, approved bills stay approved) — approval only ever
            // happens through the full write-back once every line is allocated.
            var payload = new JsonObject
            {
                [idProperty] = request.InvoiceId,
                ["LineItems"] = lineItems
            };
            using var response = await SendJsonAsync(HttpMethod.Post, token,
                $"{baseUrl}/{request.InvoiceId}", payload, "set site tracking", ct);

            // The cached snapshot now holds stale tracking for this invoice.
            _cachedSnapshot = null;
            _cachedSnapshotAt = DateTimeOffset.MinValue;

            return XeroApprovalResult.Ok(status);
        }
        catch (XeroCallFailedException failure)
        {
            return XeroApprovalResult.Failed(failure.Message);
        }
    }

    /// <summary>
    /// The target line's new tracking: the Sites entry replaced with <paramref name="siteOption"/>
    /// — or removed outright when it is null (unset) — with every other category
    /// (Xero's own Cost Code, anything else) carried over untouched.
    /// </summary>
    private static JsonArray ReplaceSiteTracking(JsonElement line, TrackingCategoryLookup categories, string? siteOption)
    {
        var tracking = new JsonArray();
        if (siteOption is not null)
            tracking.Add(new JsonObject { ["TrackingCategoryID"] = categories.SiteCategoryId, ["Option"] = siteOption });
        if (line.TryGetProperty("Tracking", out var existing) && existing.ValueKind == JsonValueKind.Array)
            foreach (var entry in existing.EnumerateArray())
            {
                var categoryId = StringOf(entry, "TrackingCategoryID");
                if (categoryId is not null
                    && !categoryId.Equals(categories.SiteCategoryId, StringComparison.OrdinalIgnoreCase))
                    tracking.Add(JsonNode.Parse(entry.GetRawText()));
            }
        return tracking;
    }

    // -- site P&L: the job's own monthly income and cost, as the accounts hold it ------

}

using System.Net.Http;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using Jewel.JPMS.Components;
using Jewel.JPMS.Cqrs;
using Jewel.JPMS.Models;
using Jewel.JPMS.Services;
using Jewel.JPMS.Services.Excel;
using Jewel.JPMS.Services.Navigation;
using Jewel.JPMS.Contracts.Requests;
using Jewel.JPMS.Contracts.Ai;
using Jewel.JPMS.Features.RecordLinks;

namespace Jewel.JPMS.Pages;

public partial class ProjectVariations
{
    [Parameter] public string ProjectId { get; set; } = "";

    private bool isLoaded;

    // The request register rides along read-only: the Request column links each variation back to
    // the RFI it prices, and the search reads the originating request's text.
    private IReadOnlyList<Request> AllRecords => RequestRegister.ForProject(ProjectId);

    // Manual variation entry (a standalone variation with no request). The number field pre-fills with the
    // project's next number but the user can set it to match a client-issued reference.
    private bool addVariationOpen;
    private int NextVariationNumber => (orders.Count == 0 ? 0 : orders.Max(o => o.Number)) + 1;
    private IReadOnlyCollection<int> UsedVariationNumbers => orders.Select(o => o.Number).ToHashSet();
    private void OpenAddVariationDialog() => addVariationOpen = true;

    private void CloseAddVariationDialog()
    {
        addVariationOpen = false;
    }

    private async Task OnManualVariationCreated(VariationOrder created)
    {
        addVariationOpen = false;
        // The new draft appears as a "No request" row in the register; approve it there (or open it)
        // to write it onto the valuation report.
        await LoadVariationsAsync();
    }

    // ---- The variation book ---------------------------------------------------------------------

    private string? variationsError;
    private IReadOnlyList<VariationOrder> orders = Array.Empty<VariationOrder>();

    // One row per variation order — the unified document from first pricing to client decision.
    private List<VariationOrder> Rows { get; set; } = new();

    private int OpenVariationsCount => orders.Count(o =>
        o.Status is VariationOrderStatus.Quoting or VariationOrderStatus.Issued
            or VariationOrderStatus.AwaitingArchitectInstruction);

    private int ApprovedVariationsCount => orders.Count(o => o.Status == VariationOrderStatus.Approved);

    private List<VariationOrder> ApprovedOrders =>
        orders.Where(o => o.Status == VariationOrderStatus.Approved).ToList();

    // The record's identifier: the "V18" VariationRef once approved, else the same number rendered
    // the same way ("V18"). One document, one number, at every stage — Reference keeps the historic
    // "VOQ-0001" spelling because it is a persisted identifier, not something a user should read.
    private static string RowReference(VariationOrder order) =>
        !string.IsNullOrWhiteSpace(order.VariationRef) ? order.VariationRef! : order.DisplayNumber;

    // The quoting estimate until approval, then the agreed (contract) value.
    private static decimal? RowValue(VariationOrder order) =>
        order.Status == VariationOrderStatus.Approved ? order.Value : order.EstimatedValue;

    // The trace back up the lifecycle: the register's request this variation prices, if its
    // RequestId resolves. Seeded variations predate the link, so a null here renders the "No request" badge.
    private Request? RequestFor(VariationOrder order) =>
        string.IsNullOrWhiteSpace(order.RequestId)
            ? null
            : AllRecords.FirstOrDefault(r => string.Equals(r.RequestId, order.RequestId, StringComparison.OrdinalIgnoreCase));

    private static string RefLabel(Request record) =>
        !string.IsNullOrWhiteSpace(record.Reference) ? record.Reference
        : record.DisplayNumber.Length > 0 ? record.DisplayNumber
        : "(no ref)";

    // Blank RequestId only — the definition both repair panels (the variation page's
    // Originating-request picker and the RFI page's Link-variation picker) use, so the banner
    // never counts a record neither of them can fix (a dangling RequestId whose request was deleted).
    private List<VariationOrder> UnlinkedRows => Rows.Where(order => string.IsNullOrWhiteSpace(order.RequestId)).ToList();

    // ---- Free-text search -----------------------------------------------------------------------
    // Keywords are ANDed on whitespace, same as every register search. The originating request's
    // own text is included — someone searching "boiler" should find the variation that priced the
    // boiler RFI even when only the RFI says the word.

    private string search = "";

    private bool Searching => !string.IsNullOrWhiteSpace(search);

    private void OnSearchInput(ChangeEventArgs e) => search = e.Value?.ToString() ?? "";

    private void ClearSearch() => search = "";

    private bool MatchesSearch(VariationOrder order)
    {
        if (!Searching) return true;

        var tokens = search.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return tokens.All(token =>
            SearchableText(order).Any(field => field is not null && field.Contains(token, StringComparison.OrdinalIgnoreCase)));
    }

    private IEnumerable<string?> SearchableText(VariationOrder order)
    {
        yield return order.Reference;
        yield return order.DisplayNumber;
        yield return order.VariationRef;
        yield return order.Title;
        yield return order.Description;
        yield return order.CostCode;

        // The Request column's own text, so the search finds a variation by its RFI.
        var source = RequestFor(order);
        if (source is null) yield break;
        yield return source.Reference;
        yield return source.DisplayNumber;
        yield return source.Title;
    }

    private IReadOnlyList<VariationOrder> FilteredRows =>
        Rows.Where(MatchesSearch).ToList();

    // ---- Subcontractor variation requests ----

    private IReadOnlyList<SubcontractorVariationRequest> variationRequests = Array.Empty<SubcontractorVariationRequest>();
    private bool requestBusy;
    private string? requestError;
    private string? rejectingRequestId;
    private string rejectReason = "";

    private List<SubcontractorVariationRequest> OpenRequests =>
        variationRequests.Where(r => r.IsOpen).ToList();

    private List<SubcontractorVariationRequest> ReviewedRequests =>
        variationRequests.Where(r => !r.IsOpen).ToList();

    // Mirrors the API's VariationRoles.AllowedToManageVariations.
    private bool CanManageVariations => Session.AvailableRoles.Any(role =>
        role is Role.Admin or Role.ManagingDirector or Role.ProjectManager or Role.QuantitySurveyor);

    // Mirrors the API's issue gate (Director/PM, like awarding a bid package).
    private bool CanIssueWorkOrders => Session.AvailableRoles.Any(role =>
        role is Role.Admin or Role.ManagingDirector or Role.ProjectManager);

    private async Task AcceptRequest(string variationRequestId)
    {
        if (requestBusy) return;
        requestError = null;
        try
        {
            requestBusy = true;
            await Variations.AcceptVariationRequestAsync(variationRequestId);
            await LoadVariationsAsync(); // The new Selected variation appears in the register below.
        }
        catch (CommandFailedException ex) { requestError = ex.Message; }
        catch { requestError = "Couldn't accept the request. Please try again."; }
        finally { requestBusy = false; }
    }

    private void StartReject(string variationRequestId)
    {
        rejectingRequestId = variationRequestId;
        rejectReason = "";
        requestError = null;
    }

    private async Task ConfirmReject(string variationRequestId)
    {
        if (requestBusy || string.IsNullOrWhiteSpace(rejectReason)) return;
        requestError = null;
        try
        {
            requestBusy = true;
            await Variations.RejectVariationRequestAsync(variationRequestId, rejectReason.Trim());
            rejectingRequestId = null;
            await LoadVariationsAsync();
        }
        catch (CommandFailedException ex) { requestError = ex.Message; }
        catch { requestError = "Couldn't reject the request. Please try again."; }
        finally { requestBusy = false; }
    }

    // ---- Issue work order for an approved variation order ----

    private IReadOnlyList<WorkOrder> IssuedWorkOrdersFor(VariationOrder order) =>
        Procurement.WorkOrdersFor(ProjectId)
            .Where(wo => string.Equals(wo.VariationOrderId, order.VariationOrderId, StringComparison.OrdinalIgnoreCase))
            .ToList();

    // A work order is only instructed after approval — the client's instruction to proceed.
    private bool CanIssueWorkOrder(VariationOrder order) =>
        CanIssueWorkOrders
        && order.Status == VariationOrderStatus.Approved
        && !string.IsNullOrWhiteSpace(order.SelectedSubcontractorId);

    private async Task IssueWorkOrder(string variationOrderId)
    {
        if (requestBusy) return;
        requestError = null;
        try
        {
            requestBusy = true;
            await Variations.IssueWorkOrderForVariationOrderAsync(variationOrderId);
            Procurement.Refresh(ProjectId); // Show the new order in the issued-WO column.
            await LoadVariationsAsync();    // VO may have moved Approved → Issued.
        }
        catch (CommandFailedException ex) { requestError = ex.Message; }
        catch { requestError = "Couldn't issue the work order. Please try again."; }
        finally { requestBusy = false; }
    }

    // ---- In-row variation status changes (the chip's dropdown) ----------------------------------

    private string? variationStatusMenuId;
    private string? variationStatusBusyId;
    private string? variationStatusError;

    private void ToggleVariationStatusMenu(string variationOrderId) =>
        variationStatusMenuId = variationStatusMenuId == variationOrderId ? null : variationOrderId;

    // One dropdown entry: a direct move (Action) or a link through to the variation (Href) for the
    // transitions whose real flows — cost code, confirms, reversals — live on the record itself.
    private sealed record VariationStatusChoice(string Label, string? Hint, bool IsCurrent, Func<Task>? Action = null, string? Href = null);

    private List<VariationStatusChoice> VariationStatusChoices(VariationOrder order)
    {
        var choices = new List<VariationStatusChoice>();
        var variationHref = $"/projects/{ProjectId}/variations/{order.VariationOrderId}";

        // Rejected is a terminal audit record — the pill doesn't offer reactivation.
        if (order.Status == VariationOrderStatus.Rejected) return choices;

        if (order.Status == VariationOrderStatus.Approved)
        {
            // An approved order can only move back to Quoting (data repair, un-approve) or to
            // Rejected (a real commercial event) — never straight across to Issued.
            choices.Add(new("Approved", null, true));
            choices.Add(new("Quoting (return to quoting)…",
                "Un-approves — reverses the approval's writes and frees the V-ref; a record correction",
                false,
                Action: () => ChangeVariationStatusInline(order, VariationOrderStatus.Quoting)));
            choices.Add(new("Rejected…",
                "A real commercial event — reverses the approval's valuation / CVR / budget writes",
                false,
                Action: () => ChangeVariationStatusInline(order, VariationOrderStatus.Rejected)));
            return choices;
        }

        // Quoting / Issued / Awaiting AI: move directly between the side-effect-free stages, approve
        // (through the variation, where the cost code and value are collected) or reject.
        choices.Add(new("Quoting", null, order.Status == VariationOrderStatus.Quoting,
            Action: () => ChangeVariationStatusInline(order, VariationOrderStatus.Quoting)));
        choices.Add(new("Issued",
            "Marks the variation as sent to the client, awaiting their decision",
            order.Status == VariationOrderStatus.Issued,
            Action: () => ChangeVariationStatusInline(order, VariationOrderStatus.Issued)));
        choices.Add(new("Awaiting AI",
            "Issued and waiting on a formal Architect's Instruction — no commercial effect yet",
            order.Status == VariationOrderStatus.AwaitingArchitectInstruction,
            Action: () => ChangeVariationStatusInline(order, VariationOrderStatus.AwaitingArchitectInstruction)));
        choices.Add(new("Approved…",
            "Approving mints the V-ref and writes the contract figures — runs on the variation itself",
            false, Href: variationHref));
        choices.Add(new("Rejected…",
            "Declined by the client or withdrawn — terminal, and confirmed before it is applied",
            false,
            Action: () => { decliningVariation = order; return Task.CompletedTask; }));
        return choices;
    }

    private async Task PickVariationStatus(VariationStatusChoice choice)
    {
        variationStatusMenuId = null;
        if (choice.IsCurrent) return;
        if (choice.Href is not null) { Nav.NavigateTo(choice.Href); return; }
        if (choice.Action is not null) await choice.Action();
    }

    // The variation the decline modal is asking about; null when the modal is closed.
    private VariationOrder? decliningVariation;

    private async Task ConfirmDeclineVariation()
    {
        if (decliningVariation is not { } order) return;
        await ChangeVariationStatusInline(order, VariationOrderStatus.Rejected);
        // Close only on success — a failure leaves the modal up with the error visible behind it,
        // rather than silently swallowing the attempt.
        if (variationStatusError is null) decliningVariation = null;
    }

    private async Task ChangeVariationStatusInline(VariationOrder order, VariationOrderStatus status)
    {
        if (variationStatusBusyId is not null) return;
        variationStatusError = null;
        try
        {
            variationStatusBusyId = order.VariationOrderId;
            if (status == VariationOrderStatus.Rejected)
                await Variations.RejectAsync(order.VariationOrderId);
            else if (status == VariationOrderStatus.Quoting && order.Status == VariationOrderStatus.Approved)
                await Variations.ReturnToQuotingAsync(order.VariationOrderId);
            else
                await Variations.SetStatusAsync(order.VariationOrderId, status);
            await LoadVariationsAsync();
        }
        catch (CommandFailedException ex) { variationStatusError = $"{RowReference(order)}: {ex.Message}"; }
        catch { variationStatusError = $"Couldn't change the status of {RowReference(order)}. Please try again."; }
        finally { variationStatusBusyId = null; }
    }

    private static string VariationStatusLabel(VariationOrder order) => order.Status switch
    {
        VariationOrderStatus.Quoting => "Quoting",
        VariationOrderStatus.Issued => "Issued",
        VariationOrderStatus.AwaitingArchitectInstruction => "Awaiting AI",
        VariationOrderStatus.Approved => "Approved",
        VariationOrderStatus.Rejected => "Rejected",
        _ => "Variation"
    };

    private static string BadgeClass(VariationOrder order)
    {
        const string baseClass = "inline-flex items-center rounded-full border px-2 py-0.5 text-[11px] font-medium ";
        return order.Status switch
        {
            VariationOrderStatus.Approved => baseClass + "bg-accent/10 border-accent/30 text-accent",
            VariationOrderStatus.Rejected => baseClass + "bg-negative/10 border-negative/30 text-negative",
            _ => baseClass + "bg-surface-raised border-line text-content-muted"
        };
    }


    // ---- Excel export ----------------------------------------------------------------------
    // One Variations sheet — the current view (following the search); "Include entire register"
    // from the export menu overrides the search and takes every row.

    private ExcelWorkbook? BuildExportWorkbook(bool includeEntireRegister)
    {
        var workbook = new ExcelWorkbook();

        var variationRows = includeEntireRegister ? (IReadOnlyList<VariationOrder>)Rows : FilteredRows;
        if (variationRows.Count > 0)
        {
            var sheet = workbook.AddSheet("Variations",
                new ExcelColumn("Ref"),
                new ExcelColumn("Title"),
                new ExcelColumn("Request"),
                new ExcelColumn("Status"),
                new ExcelColumn("Value", ExcelFormat.Currency),
                new ExcelColumn("Issued", ExcelFormat.Date),
                new ExcelColumn("Approved", ExcelFormat.Date),
                new ExcelColumn("Created", ExcelFormat.Date),
                new ExcelColumn("Work order"));

            foreach (var order in variationRows)
            {
                var sourceRequest = RequestFor(order);
                var issued = IssuedWorkOrdersFor(order);
                sheet.AddRow(
                    RowReference(order),
                    order.Title,
                    sourceRequest is null ? null : RefLabel(sourceRequest),
                    VariationStatusLabel(order),
                    RowValue(order),
                    order.IssuedAt?.LocalDateTime,
                    order.Status == VariationOrderStatus.Approved ? order.ApprovedAt?.LocalDateTime : null,
                    order.CreatedAt.LocalDateTime,
                    issued.Count == 0 ? null : string.Join(", ", issued.Select(wo => $"WO-{wo.Number:0000}")));
            }
        }

        return workbook.Sheets.Count == 0 ? null : workbook;
    }

    private async Task LoadVariationsAsync()
    {
        try
        {
            orders = await Variations.ListForProjectAsync(ProjectId);
            variationRequests = await Variations.ListVariationRequestsForProjectAsync(ProjectId);
            Rows = orders.OrderByDescending(order => order.Number).ToList();
            variationsError = null;
        }
        catch { variationsError = "Couldn't load variations. Please try again."; }
    }

    protected override async Task OnInitializedAsync()
    {
        await Session.EnsureLoadedAsync();
        if (!Auth.IsSignedIn) { Nav.NavigateTo("/login", forceLoad: true); return; }
        RequestRegister.OnChange += StateHasChanged;
        Activity.OnChanged += StateHasChanged;
        // Refresh on entry (stale-while-revalidate): cached data renders immediately, then
        // updates when the background reload lands.
        RequestRegister.Refresh(ProjectId); // The Request column + search read the register.
        Procurement.Refresh(ProjectId);     // Background revalidation of work orders (issued-WO column).
        Activity.Refresh(ProjectId);        // Activity badges land in the background — absent until then.
        await LoadVariationsAsync();
        isLoaded = true;
    }

    public void Dispose()
    {
        RequestRegister.OnChange -= StateHasChanged;
        Activity.OnChanged -= StateHasChanged;
    }
}

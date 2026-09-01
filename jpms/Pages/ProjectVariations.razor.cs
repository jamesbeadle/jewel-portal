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

}

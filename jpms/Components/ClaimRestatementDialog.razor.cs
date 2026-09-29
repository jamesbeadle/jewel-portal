using Jewel.JPMS.Commercial;
using Jewel.JPMS.Contracts.Commercial;
using Jewel.JPMS.Cqrs;
using Jewel.JPMS.Models;
using Jewel.JPMS.Services;
using Microsoft.AspNetCore.Components;
using static Jewel.JPMS.Features.Commercial.ValuationReportDisplay;

namespace Jewel.JPMS.Components;

public partial class ClaimRestatementDialog
{
    [Parameter] public string ProjectId { get; set; } = "";
    [Parameter] public bool IsOpen { get; set; }
    /// <summary>The locked claim whose lines are restated — must be Preapproved or Confirmed to save.</summary>
    [Parameter] public ValuationClaim? Claim { get; set; }
    [Parameter] public EventCallback OnClose { get; set; }
    [Parameter] public EventCallback OnSaved { get; set; }

    private sealed class PendingEntry
    {
        public string ValuationLineItemId { get; set; } = "";
        public string PercentText { get; set; } = "";
    }

    private readonly List<PendingEntry> Pending = new();
    private IReadOnlyList<ValuationStatementLine> statementLines = Array.Empty<ValuationStatementLine>();
    private string pickedLineId = "";
    private string? error;
    private bool saving;
    private bool wasOpen;

    private bool ClaimIsLocked => Claim is { IsLocked: true };

    protected override async Task OnParametersSetAsync()
    {
        var isOpening = IsOpen && !wasOpen;
        wasOpen = IsOpen;
        if (!isOpening) return;
        Pending.Clear();
        error = null;
        pickedLineId = "";
        statementLines = await StatementLinesAsync();
    }

    /// <summary>The frozen rows are the lines being restated — never the live bill, which may since have moved.</summary>
    private async Task<IReadOnlyList<ValuationStatementLine>> StatementLinesAsync()
    {
        if (Claim is null || !ClaimIsLocked) return Array.Empty<ValuationStatementLine>();
        var statement = await Store.GetStatementAsync(Claim.ValuationClaimId);
        return statement.Lines.Where(line => line.CountsTowardTotals).ToList();
    }

    private IReadOnlyList<SearchSelect.Option> LineOptions =>
        statementLines
            .Where(line => Pending.All(entry => entry.ValuationLineItemId != line.ValuationLineItemId))
            .Select(line => new SearchSelect.Option(line.ValuationLineItemId, $"{LabelFor(line)} — {Pct(line.PercentComplete)}"))
            .ToList();

    private void AddPicked(string lineId)
    {
        pickedLineId = "";
        var line = LineFor(lineId);
        if (line is null || Pending.Any(entry => entry.ValuationLineItemId == lineId)) return;
        Pending.Add(new PendingEntry { ValuationLineItemId = lineId, PercentText = PercentText(line.PercentComplete) });
    }

    private ValuationStatementLine? LineFor(string lineId) =>
        statementLines.FirstOrDefault(line => line.ValuationLineItemId == lineId);

    private static string LabelFor(ValuationStatementLine? line) =>
        line is null ? "(line not on this statement)"
        : line.ElementType == ValuationElementType.Variation ? $"{line.VariationRef} · {line.Description}"
        : $"{(string.IsNullOrWhiteSpace(line.CostCode) ? line.SectionName : line.CostCode)} · {line.Description}";

    private static decimal PercentOf(PendingEntry row)
    {
        return FormNumber.TryParseAmount(row.PercentText, out var percent) ? percent : 0m;
    }

    private decimal NewMoneyFor(PendingEntry row) =>
        ValuationCalculations.CumulativeClaimed(PercentOf(row), LineFor(row.ValuationLineItemId)?.LineAmount ?? 0m);

    private ClaimRestatement.Outcome Outcome => ClaimRestatement.Plan(
        Pending.Select(row => LineFor(row.ValuationLineItemId)).OfType<ValuationStatementLine>().Select(ToRestatementRow).ToList(),
        Pending.ToDictionary(row => row.ValuationLineItemId, PercentOf));

    private static ClaimRestatement.Row ToRestatementRow(ValuationStatementLine line) =>
        new(line.ValuationLineItemId, line.LineAmount, line.CountsTowardTotals,
            line.ElementType == ValuationElementType.Variation, line.CumulativeClaimed);

    private bool KeepsTheTotal => Outcome.KeepsTheTotal;

    private async Task Cancel() => await OnClose.InvokeAsync();

    private async Task SaveAsync()
    {
        if (saving || Claim is null || !ClaimIsLocked || !KeepsTheTotal) return;
        error = null;
        var entries = Pending.Select(row => new ClaimEntryInput(row.ValuationLineItemId, PercentOf(row))).ToList();
        saving = true;
        try
        {
            await Store.RestateLinesAsync(ProjectId, new RestateValuationClaimLines(Claim.ValuationClaimId, entries));
            await OnSaved.InvokeAsync();
        }
        catch (CommandFailedException failure) { error = failure.Message; }
        catch { error = "Couldn't restate the lines. Please try again."; }
        finally { saving = false; }
    }
}

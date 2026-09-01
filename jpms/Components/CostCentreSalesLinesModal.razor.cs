using Jewel.JPMS.Services.Excel;
using static Jewel.JPMS.MoneyFormats;
using Jewel.JPMS.Contracts.Commercial;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Components;

public partial class CostCentreSalesLinesModal
{
    [Parameter] public bool IsOpen { get; set; }
    [Parameter] public string ProjectId { get; set; } = "";

    /// <summary>Modal heading: "CODE — Name" for one centre, or the roll-up's name.</summary>
    [Parameter] public string Heading { get; set; } = "";

    /// <summary>The cost codes behind the clicked figure — one for an individual row,
    /// all members for a roll-up (the Centre column appears when there are several).</summary>
    [Parameter] public IReadOnlyList<string> CostCodes { get; set; } = Array.Empty<string>();

    [Parameter] public IReadOnlyList<ValuationLineItem> Lines { get; set; } = Array.Empty<ValuationLineItem>();
    [Parameter] public EventCallback OnClose { get; set; }

    /// <summary>Raised after a line is recoded to another centre. The store has already
    /// refreshed the valuation lines; the page re-pulls the financial summary so the
    /// Contract Sales Value figures move with the line. The modal stays open.</summary>
    [Parameter] public EventCallback OnLineRecoded { get; set; }

    // The centre column earns its place either for a roll-up (several codes to tell
    // apart) or because the signed-in role may recode lines — a single-centre modal
    // otherwise has nothing to edit.
    private bool ShowCentreColumn => CostCodes.Count > 1 || CanRecodeCostCentres;

    private List<ValuationLineItem> LinesForCentres =>
        Lines.Where(line => CostCodes.Contains(line.CostCode, StringComparer.OrdinalIgnoreCase))
            .OrderBy(line => line.CostCode, StringComparer.OrdinalIgnoreCase)
            .ThenBy(line => line.DisplayOrder)
            .ToList();

    private List<ValuationLineItem> CountingLines =>
        LinesForCentres.Where(line => line.CountsTowardTotals).ToList();

    private List<ValuationLineItem> NonCountingLines =>
        LinesForCentres.Where(line => !line.CountsTowardTotals).ToList();

    // Variation lines mirror an approved VO whose descriptive text lives in VariationTitle;
    // older lines have an empty Description, so fall back to the title (same rule as the
    // Valuation Report tab's TitleFor) instead of rendering a blank cell.
    private static string DescriptionFor(ValuationLineItem line) =>
        line.ElementType == ValuationElementType.Variation && string.IsNullOrWhiteSpace(line.Description)
            ? line.VariationTitle
            : line.Description;

    // Mirrors the Valuation Report tab's CodeFor so the two views cross-reference —
    // finance reads this list against that tab, so a ref only this modal uses is a
    // dead end. Works/PC/contingency lines show the cost code (NRM2 section ref only
    // as a fallback); variations show their V-ref. The bill section survives in the
    // tooltip.
    private static string Ref(ValuationLineItem line) =>
        line.ElementType == ValuationElementType.Variation
            ? (string.IsNullOrWhiteSpace(line.VariationRef) ? line.CostCode : line.VariationRef)
            : (string.IsNullOrWhiteSpace(line.CostCode) ? line.SectionCode : line.CostCode);

    private static string RefTitle(ValuationLineItem line) =>
        line.ElementType == ValuationElementType.Variation
            ? line.VariationTitle
            : string.IsNullOrWhiteSpace(line.SectionCode)
                ? line.SectionName
                : $"{line.SectionCode} — {line.SectionName}";

    private static string TypeLabel(ValuationLineItem line) => line.ElementType switch
    {
        ValuationElementType.Variation => "Variation",
        ValuationElementType.PcSum => "PC sum",
        ValuationElementType.Contingency => "Contingency",
        _ => line.LineType switch
        {
            ValuationLineType.ProvisionalSum => "Provisional sum",
            ValuationLineType.Omit => "Omit",
            _ => "Contract works"
        }
    };


    // ---- Export to Excel ---------------------------------------------------
    // Exports exactly what the modal shows, in the modal's order: the counting
    // lines, the total row, then (when present) the recorded-but-not-priced lines
    // under the same section label. The Centre column travels only when it's on
    // screen, so the sheet reads against the modal one-for-one. The modal has no
    // filters, so the include-all menu row isn't offered and the flag is ignored.
    private ExcelWorkbook? BuildExportWorkbook(bool includeAllRows)
    {
        var counting = CountingLines;
        var nonCounting = NonCountingLines;
        if (counting.Count == 0 && nonCounting.Count == 0) return null;

        var workbook = new ExcelWorkbook();
        var sheet = ShowCentreColumn
            ? workbook.AddSheet("Contract sales value",
                new ExcelColumn("Ref"),
                new ExcelColumn("Centre"),
                new ExcelColumn("Description"),
                new ExcelColumn("Type"),
                new ExcelColumn("Amount", ExcelFormat.Currency))
            : workbook.AddSheet("Contract sales value",
                new ExcelColumn("Ref"),
                new ExcelColumn("Description"),
                new ExcelColumn("Type"),
                new ExcelColumn("Amount", ExcelFormat.Currency));

        foreach (var line in counting)
        {
            sheet.AddRow(ExportRow(line, TypeLabel(line)));
        }

        var total = new object?[sheet.Columns.Count];
        total[ShowCentreColumn ? 2 : 1] = "Total — contract sales value";
        total[^1] = counting.Sum(line => line.LineAmount);
        sheet.AddRow(total);

        if (nonCounting.Count > 0)
        {
            sheet.AddRow();
            sheet.AddRow("Recorded but not priced into the total");
            foreach (var line in nonCounting)
            {
                sheet.AddRow(ExportRow(line,
                    line.LineType == ValuationLineType.Tbc ? "TBC" : "Declined"));
            }
        }

        return workbook;
    }

    private object?[] ExportRow(ValuationLineItem line, string typeLabel) =>
        ShowCentreColumn
            ? new object?[] { Ref(line), line.CostCode, DescriptionFor(line), typeLabel, line.LineAmount }
            : new object?[] { Ref(line), DescriptionFor(line), typeLabel, line.LineAmount };

    // ---- Cost-centre recode ------------------------------------------------
    // Mirrors the API gate (ValuationReportAuthorisation.RolesThatMayRecodeCostCentres)
    // and the Valuation Report tab's editor: admins, the MD, the FD and project
    // managers may move a line's value between cost centres; everyone else sees the
    // allocation read-only.
    private bool CanRecodeCostCentres => Session.ActiveRole
        is Role.Admin or Role.ManagingDirector or Role.FinanceDirector or Role.ProjectManager;

    private string? recodingLineId;
    private string? costCentreError;
    private string? costCentreErrorLineId;

    private async Task SetCostCentreAsync(ValuationLineItem line, string code)
    {
        // SearchSelect's leading blank entry clears — a line's value always sits
        // somewhere, so ignore it rather than send an update the API would reject.
        if (string.IsNullOrWhiteSpace(code) || code == line.CostCode) return;
        recodingLineId = line.ValuationLineItemId;
        costCentreError = null;
        costCentreErrorLineId = null;
        try
        {
            await Store.SetLineCostCentreAsync(new SetValuationLineCostCentre(line.ValuationLineItemId, code));
            await OnLineRecoded.InvokeAsync();
        }
        catch (CommandFailedException failure)
        {
            costCentreError = failure.Message;
            costCentreErrorLineId = line.ValuationLineItemId;
        }
        catch
        {
            // Transport/API failure (e.g. the API mid-deploy answering 503) must not kill
            // the whole app — surface it on the row and let the user simply retry.
            costCentreError = "The server couldn't be reached — please try again in a moment.";
            costCentreErrorLineId = line.ValuationLineItemId;
        }
        finally
        {
            recodingLineId = null;
        }
    }

    // The label carries code + name so typing matches either (same as the Valuation
    // Report tab and XeroAllocation).
    private IReadOnlyList<SearchSelect.Option>? costCentreOptionsCache;
    private object? costCentreOptionsCacheKey;

    private IReadOnlyList<SearchSelect.Option> CostCentreOptions
    {
        get
        {
            var centres = CostCenters.ActiveAlphabetical();
            if (costCentreOptionsCache is null || !ReferenceEquals(costCentreOptionsCacheKey, centres))
            {
                costCentreOptionsCache = centres
                    .Select(centre => new SearchSelect.Option(centre.Code, $"{centre.Code} {centre.Name}")).ToList();
                costCentreOptionsCacheKey = centres;
            }
            return costCentreOptionsCache;
        }
    }

    // A line coded to a retired centre still shows (and can be moved off) it.
    private IReadOnlyList<SearchSelect.Option> CostCentreOptionsFor(ValuationLineItem line)
    {
        var options = CostCentreOptions;
        if (string.IsNullOrWhiteSpace(line.CostCode) || options.Any(option => option.Value == line.CostCode))
            return options;
        var withCurrent = new List<SearchSelect.Option>(options.Count + 1)
        {
            new(line.CostCode, $"{line.CostCode} (retired)")
        };
        withCurrent.AddRange(options);
        return withCurrent;
    }
}

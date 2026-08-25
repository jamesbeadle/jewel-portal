using Jewel.JPMS.Models;

namespace Jewel.JPMS.Features.Commercial;

/// <summary>One editable row of the client-references dialog: a cost centre and the client's
/// reference for it. Mutable because the input binds straight to it.</summary>
public sealed class ClientCostReferenceRow
{
    public string CostCode { get; init; } = "";
    public string Name { get; init; } = "";
    public string ClientReference { get; set; } = "";
    // False for a mapping whose cost centre no longer appears on any valuation line — still
    // shown so it can be seen and cleared, never silently kept.
    public bool IsOnReport { get; init; }

    public ClientCostReferenceEntry ToEntry() => new(CostCode, ClientReference);
}

/// <summary>
/// Builds the dialog's rows: every cost centre the project's valuation lines sell against (in
/// master order, named from the master), then any leftover mapping for a centre no longer on
/// the report. Variation lines count — they carry a cost centre and print on the PDF too.
/// </summary>
public static class ClientCostReferenceRows
{
    public static IReadOnlyList<ClientCostReferenceRow> Build(
        IReadOnlyList<ValuationLineItem> lines,
        IReadOnlyList<CostCenter> costCentres,
        IReadOnlyList<ClientCostReference> references)
    {
        var referenceByCode = references
            .GroupBy(reference => reference.CostCode, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First().ClientReference, StringComparer.OrdinalIgnoreCase);
        var codesOnReport = lines
            .Select(line => line.CostCode.Trim())
            .Where(code => code.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        var masterOrder = costCentres
            .Select((centre, index) => (centre.Code, index))
            .GroupBy(pair => pair.Code, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First().index, StringComparer.OrdinalIgnoreCase);
        string NameFor(string code) =>
            costCentres.FirstOrDefault(centre => string.Equals(centre.Code, code, StringComparison.OrdinalIgnoreCase))?.Name ?? "";

        var rows = codesOnReport
            .OrderBy(code => masterOrder.GetValueOrDefault(code, int.MaxValue))
            .ThenBy(code => code, StringComparer.OrdinalIgnoreCase)
            .Select(code => new ClientCostReferenceRow
            {
                CostCode = code,
                Name = NameFor(code),
                ClientReference = referenceByCode.GetValueOrDefault(code, ""),
                IsOnReport = true
            })
            .ToList();

        var leftovers = referenceByCode.Keys
            .Where(code => !codesOnReport.Contains(code, StringComparer.OrdinalIgnoreCase))
            .OrderBy(code => code, StringComparer.OrdinalIgnoreCase)
            .Select(code => new ClientCostReferenceRow
            {
                CostCode = code,
                Name = NameFor(code),
                ClientReference = referenceByCode[code],
                IsOnReport = false
            });
        rows.AddRange(leftovers);
        return rows;
    }
}

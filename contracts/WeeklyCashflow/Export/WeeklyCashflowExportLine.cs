namespace Jewel.JPMS.Contracts.WeeklyCashflow.Export;

/// <summary>
/// One line of an exported band — a supplier group, a single supplier or client, or one manual
/// item — and the entries it stands for. The plan tab prints the line alone; the detail tab
/// prints the line and then each entry beneath it. A cell index runs over the week axis with
/// the Later bucket last, exactly as <see cref="WeeklyCashflowView"/> counts its columns.
/// </summary>
public sealed record WeeklyCashflowExportLine(string Label, IReadOnlyList<WeeklyCashflowEntry> Entries)
{
    public decimal AmountIn(int cellIndex) =>
        Entries
            .Where(entry => entry.WeekIndex == cellIndex)
            .Sum(entry => entry.Amount);

    /// <summary>A cell holding any entry the accountant moved here reads as moved, as on screen.</summary>
    public bool HasMovedEntryIn(int cellIndex) =>
        Entries.Any(entry => entry.WeekIndex == cellIndex && entry.Moved);

    public decimal Total => Entries.Sum(entry => entry.Amount);
}

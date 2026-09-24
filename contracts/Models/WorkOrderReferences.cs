using System.Text.RegularExpressions;

namespace Jewel.JPMS.Models;

/// <summary>
/// The one spelling of a work order's reference. Order numbers are minted PER PROJECT, so
/// "WO-0054" names an order on By France and another on JBB-2026-005; every reference the portal
/// shows or sends carries its project — "JBB-2026-001-WO-0054" — so a number on an invoice, a
/// statement or in conversation points at exactly one order (Jeremy, 2026-09-24). The short form
/// is what people still say, and the lookups still take it. The qualified reference is the same
/// spelling as the order's mailbox tag stem.
/// </summary>
public static class WorkOrderReferences
{
    public const string Prefix = "WO-";

    public static string Short(int number) => $"{Prefix}{number:0000}";

    public static string Qualified(string? projectReference, int number) => Qualify(projectReference, Short(number));

    /// <summary>A project with no reference yet leaves the short form as it is.</summary>
    public static string Qualify(string? projectReference, string shortReference) =>
        string.IsNullOrWhiteSpace(projectReference) ? shortReference : $"{projectReference.Trim()}-{shortReference}";

    private static readonly Regex Spoken = new(
        @"^\s*(?:(?<project>.*?)[-\s]*)?\bWO[-\s]*0*(?<number>\d+)\s*$|^\s*0*(?<number>\d+)\s*$",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    /// <summary>Reads a reference however it is said — "JBB-2026-001-WO-0054", "WO-0054", "wo54",
    /// "54". ProjectReference is null for the short forms, so the caller resolves the project.</summary>
    public static bool TryRead(string? text, out string? projectReference, out int number)
    {
        projectReference = null;
        number = 0;
        var match = Spoken.Match(text ?? "");
        if (!match.Success || !int.TryParse(match.Groups["number"].Value, out number) || number <= 0) return false;
        var project = match.Groups["project"].Value.Trim().TrimEnd('-').Trim();
        projectReference = project.Length == 0 ? null : project;
        return true;
    }
}

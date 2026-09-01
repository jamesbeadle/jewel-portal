
namespace Jewel.JPMS.Components;

public partial class PurchaseOrderSheet
{
    /// <summary>The order being printed.</summary>
    [Parameter, EditorRequired] public WorkOrder Order { get; set; } = default!;

    /// <summary>The order's priced lines (may be empty for orders without a breakdown).</summary>
    [Parameter] public IReadOnlyList<WorkOrderLine> Lines { get; set; } = Array.Empty<WorkOrderLine>();

    [Parameter] public string SupplierName { get; set; } = "";
    [Parameter] public string SupplierContactName { get; set; } = "";

    /// <summary>The supplier's postal address as stacked letter lines (street / town / county /
    /// postcode), printed one per line in the Sub/Vendor block. Hosts pass the directory
    /// record's AddressLines; blanks are skipped.</summary>
    [Parameter] public IReadOnlyList<string> SupplierAddressLines { get; set; } = Array.Empty<string>();
    [Parameter] public string ProjectName { get; set; } = "";

    /// <summary>The site address printed under the job name as stacked letter lines
    /// (street / town / postcode), one per line like the supplier's address — resolved by hosts
    /// from the project record (AddressLine / Town / Postcode). Blanks are skipped; empty hides it.</summary>
    [Parameter] public IReadOnlyList<string> SiteAddressLines { get; set; } = Array.Empty<string>();

    /// <summary>Display name of whoever raised/approved the order in the system
    /// (falls back to the raw AwardedByEmail when blank).</summary>
    [Parameter] public string ApprovedByName { get; set; } = "";

    /// <summary>The supplier's payment terms, printed in the Invoice and Payment Requirements
    /// section ("30 day terms"). Hosts resolve it from the subcontractor record; 30 is the
    /// business default every record starts on.</summary>
    [Parameter] public int PaymentTermsDays { get; set; } = 30;

    // Order.Reference reads "Draft" for an unreleased order (no number is minted until
    // release) and falls back to an id stem for any other unnumbered order.
    private string Reference => Order.Reference;

    // A deposit renders only when the order both requires one and carries the percentage —
    // the flag without a figure would print an empty promise.
    private bool HasDeposit => Order.DepositRequired && Order.DepositPercent is not null;

    private bool HasProgramme =>
        Order.ProgrammeStart is not null
        || Order.ScheduledCompletion is not null
        || !string.IsNullOrWhiteSpace(Order.ProgrammeNotes);

    private decimal TotalPaid => Lines.Sum(line => line.PaidToDate);

    // The signature always shows a person's NAME, never an email address — internal staff
    // sometimes sign in with addresses that don't read as names. Prefer the resolved display
    // name; otherwise humanise the email's local part ("nigel.reilly@…" → "Nigel Reilly").
    // A display name that is itself an email (the auth fallback) gets the same treatment.
    private static string SignatureName(string displayName, string email)
    {
        var name = string.IsNullOrWhiteSpace(displayName) ? email : displayName;
        if (string.IsNullOrWhiteSpace(name)) return "";
        var atIndex = name.IndexOf('@');
        if (atIndex < 0) return name.Trim();
        var words = name[..atIndex]
            .Split(new[] { '.', '_', '-', '+' }, StringSplitOptions.RemoveEmptyEntries)
            .Where(word => word.Any(char.IsLetter))
            .Select(word => char.ToUpperInvariant(word[0]) + word[1..].ToLowerInvariant());
        var humanised = string.Join(" ", words);
        return string.IsNullOrWhiteSpace(humanised) ? "Jewel Bespoke Build" : humanised;
    }


    // "25%" / "12.5%" — no trailing zeros, matching how the percentage was typed.
    private static string Percent(decimal value) =>
        value.ToString("0.##", System.Globalization.CultureInfo.GetCultureInfo("en-GB")) + "%";
}

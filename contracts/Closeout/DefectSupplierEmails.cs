using System.Globalization;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Closeout;

/// <summary>An email to a defect's supplier as the portal words it: where it goes, its subject
/// and its plain-text body. The defect page pre-fills its composer from this and the sender
/// edits; the connector's send_defect_to_supplier sends it as it stands unless the caller
/// supplies other wording.</summary>
public sealed record DefectSupplierEmail(string To, string Subject, string Body);

/// <summary>
/// The wording of the emails the portal sends a defect's supplier — the first send and the chase
/// — in one place shared by the defect page and the connector, so both say the same thing. The
/// subject leads with the reference so the supplier's reply keeps it in view, and the sent copy
/// carries the JPMS/DEF-#### tag, so that reply files itself back onto the defect.
/// </summary>
public static class DefectSupplierEmails
{
    private static readonly CultureInfo British = CultureInfo.GetCultureInfo("en-GB");
    private const string DateFormat = "d MMM yyyy";

    /// <summary>The email the defect needs next: the chase once it has been sent, else the first send.</summary>
    public static DefectSupplierEmail Next(Defect defect, string? projectName, string? projectReference, string? supplierContactName) =>
        defect.HasBeenSentToSupplier
            ? Chase(defect, projectName, supplierContactName)
            : Raise(defect, projectName, projectReference, supplierContactName);

    /// <summary>The first email to the supplier — the defect as it stands, with the ask.</summary>
    public static DefectSupplierEmail Raise(Defect defect, string? projectName, string? projectReference, string? supplierContactName)
    {
        var projectLabel = string.IsNullOrWhiteSpace(projectName) ? "" : $" at {projectName} ({projectReference})";
        var body =
            $"{Greeting(supplierContactName)}\n\n"
            + $"We have logged the following defect{projectLabel} and need it put right:\n\n"
            + $"{defect.Reference}\n"
            + (string.IsNullOrWhiteSpace(defect.Location) ? "" : $"Location: {defect.Location}\n")
            + $"Description: {defect.Description}\n\n"
            + "Please confirm when you can attend and rectify this. Reply to this email so your response stays on file against the defect.\n\n"
            + "Kind regards,\n";
        return new DefectSupplierEmail(defect.SupplierEmail, Subject(defect, projectName), body);
    }

    /// <summary>A chase — the defect was sent already; this asks for the response that hasn't come.</summary>
    public static DefectSupplierEmail Chase(Defect defect, string? projectName, string? supplierContactName)
    {
        var sent = defect.SentToSupplierAt is { } at ? $" sent to you on {at.ToString(DateFormat, British)}" : "";
        var body =
            $"{Greeting(supplierContactName)}\n\n"
            + $"A reminder about defect {defect.Reference}{sent}"
            + (string.IsNullOrWhiteSpace(defect.Location) ? "" : $" ({defect.Location})")
            + ":\n\n"
            + $"{defect.Description}\n\n"
            + "Please let us know when this will be attended to.\n\n"
            + "Kind regards,\n";
        return new DefectSupplierEmail(defect.SupplierEmail, "Chasing: " + Subject(defect, projectName), body);
    }

    private static string Greeting(string? supplierContactName) =>
        string.IsNullOrWhiteSpace(supplierContactName) ? "Hello," : $"Hello {supplierContactName.Split(' ')[0]},";

    private static string Subject(Defect defect, string? projectName)
    {
        var parts = new List<string> { defect.Reference };
        if (!string.IsNullOrWhiteSpace(defect.Location)) parts.Add(defect.Location);
        if (!string.IsNullOrWhiteSpace(projectName)) parts.Add(projectName);
        return string.Join(" · ", parts);
    }
}

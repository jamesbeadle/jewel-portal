using Jewel.JPMS.Contracts.Closeout;
using Jewel.JPMS.Features.Triage;

namespace Jewel.JPMS.Features.Closeout;

// The supplier side of a defect on the client: the directory pool a defect can be raised with,
// and the email the defect page drafts to that supplier. One place, so the register's raise
// form, the page's Edit dialog and the page's "Send to supplier" all agree on who counts as a
// supplier and what the first email says.
public static class DefectSuppliers
{
    /// <summary>The picker pool: vetted directory records of the Subcontractor and Supplier
    /// categories (never prospects, never clients or architects), by company name.</summary>
    public static IReadOnlyList<SearchSelect.Option> Options(IEnumerable<Subcontractor> directory) =>
        directory
            .Where(s => !s.IsProspect && s.Category is DirectoryCategory.Subcontractor or DirectoryCategory.Supplier)
            .OrderBy(s => s.CompanyName, StringComparer.OrdinalIgnoreCase)
            .Select(s => new SearchSelect.Option(s.SubcontractorId,
                string.IsNullOrWhiteSpace(s.ContactEmail) ? $"{s.CompanyName} (no email on record)" : s.CompanyName))
            .ToList();

    /// <summary>The first email to the supplier — the defect as it stands, with the ask. Plain
    /// text: the composer turns it into HTML and the sender edits before sending. The wording
    /// is DefectSupplierEmails (contracts), shared with the connector's send_defect_to_supplier.</summary>
    public static ComposePrefill RaiseEmail(Defect defect, Project? project, Subcontractor? supplier) =>
        Prefill(DefectSupplierEmails.Raise(defect, project?.Name, project?.Reference, supplier?.ContactName));

    /// <summary>A chase — the defect was sent already; this asks for the response that hasn't come.</summary>
    public static ComposePrefill ChaseEmail(Defect defect, Project? project, Subcontractor? supplier) =>
        Prefill(DefectSupplierEmails.Chase(defect, project?.Name, supplier?.ContactName));

    private static ComposePrefill Prefill(DefectSupplierEmail email) =>
        new(To: email.To, Subject: email.Subject, Body: email.Body);
}

namespace Jewel.JPMS.Contracts.MailboxCompose;

/// <summary>
/// The rules an outbound subject line follows, wherever the portal composes one.
///
/// A record raised from a correspondent's own paperwork often carries the site in its title
/// already — a supplier's quote becomes the work order "Tiling Adhesive Supply, By France" — and
/// appending the project to that read "… By France — By France" on every purchase order the
/// supplier received. The same fault reached the RFI, NOD and EOT documents, whose subject named
/// the project unconditionally. One rule, read by every builder, so a subject never says the
/// project twice.
/// </summary>
public static class SubjectLine
{
    public static string NamingTheProjectOnce(string title, string projectName)
    {
        if (string.IsNullOrWhiteSpace(projectName)) return title;
        if (title.Contains(projectName, StringComparison.OrdinalIgnoreCase)) return title;
        return $"{title} — {projectName}";
    }
}

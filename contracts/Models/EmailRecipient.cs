namespace Jewel.JPMS.Models;

/// <summary>Where a suggested email recipient comes from — shown as the chip's small caption so
/// two "John Smith"s can be told apart, and used to order the address book (staff first).</summary>
public enum EmailRecipientKind
{
    Staff = 0,
    Client = 1,
    Architect = 2,
    ProjectContact = 3,
    Company = 4,
    Worker = 5
}

/// <summary>
/// One line of the portal's address book — everyone the directory knows an email address for,
/// flattened for the composers' To/Cc/Bcc pickers (decision 2026-09-03: recipients come from the
/// portal's own directory, not from Outlook contacts, so no Graph Contacts permission is needed).
/// Organisation is the company / client account / practice the person belongs to; Detail is the
/// role or purpose on that record ("Accounts", "Quantity Surveyor", "Site Manager") when known.
/// </summary>
public sealed record EmailRecipient(
    string Name,
    string Email,
    string? Organisation,
    EmailRecipientKind Kind,
    string? Detail = null)
{
    /// <summary>"Name &lt;email&gt;" — the form the composers' recipient fields carry, which
    /// MailCompose.ParseRecipients reduces back to the bare address on send.</summary>
    public string Addressed => string.IsNullOrWhiteSpace(Name) || Name.Contains('<') || Name.Contains('>')
        ? Email
        : $"{Name} <{Email}>";
}

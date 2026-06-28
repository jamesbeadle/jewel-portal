namespace Jewel.JPMS.Models;

// The external parties attached to a project — the clients, architects, consultants and
// engineers an RFI/request is issued to. A project's request documents are emailed to the
// contacts whose <see cref="ProjectContact.ReceivesRequests"/> flag is set, so a project must
// carry at least one such contact before the auto-send on creation has anywhere to go.
public enum ProjectContactRole
{
    Client = 0,
    Architect = 1,
    Consultant = 2,
    Engineer = 3,
    Contractor = 4,
    Other = 5
}

public sealed record ProjectContact(
    string ContactId,
    string ProjectId,
    string Name,
    string Email,
    string? Organisation,
    ProjectContactRole Role,
    bool ReceivesRequests,
    DateTimeOffset CreatedAt);

public static class ProjectContactRoleExtensions
{
    public static string DisplayName(this ProjectContactRole role) => role switch
    {
        ProjectContactRole.Client      => "Client",
        ProjectContactRole.Architect   => "Architect",
        ProjectContactRole.Consultant  => "Consultant",
        ProjectContactRole.Engineer    => "Engineer",
        ProjectContactRole.Contractor  => "Contractor",
        _ => "Contact"
    };
}

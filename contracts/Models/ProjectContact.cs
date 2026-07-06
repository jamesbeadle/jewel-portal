namespace Jewel.JPMS.Models;

// The people attached to a project's correspondence profile — external parties (clients,
// architects, consultants, engineers) and ad-hoc recipients such as internal Jewel staff.
// Each row carries a CorrespondenceRouting: To rows are the fallback correspondent when no party
// link resolves, Cc/Bcc rows are copied on every issued request document. A row may be linked to
// a PartyContact (PartyContactId set), in which case its routing overrides that person's default
// for this project only, or ad-hoc (PartyContactId null) with its own name/email.
// ReceivesRequests is the legacy flag, kept in step with Routing == To until fully retired.
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
    DateTimeOffset CreatedAt,
    CorrespondenceRouting Routing = CorrespondenceRouting.None,
    string? PartyContactId = null);

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

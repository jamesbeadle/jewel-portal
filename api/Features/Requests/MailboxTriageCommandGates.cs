using Jewel.JPMS.Contracts.Requests;

namespace Jewel.JPMS.Api.Features.Requests;

// Gate classes for the triage commands the Control Centre sends, added 2026-08-31 so the
// connector's action gateway can compose them (docs/ai/11 §4). The MailboxTriageEndpoints keep
// their shared Gate() check: both sides read the SAME RoleSet constant
// (TriageRoles.AllowedToTriage), so there is one source of truth.

public sealed class DiscardMessageAuthorisation
{
    public bool Allows(SignedInUser user, DiscardMessage command) =>
        TriageRoles.AllowedToTriage.IncludesAny(user.Roles);
}

public sealed class RestoreMessageAuthorisation
{
    public bool Allows(SignedInUser user, RestoreMessage command) =>
        TriageRoles.AllowedToTriage.IncludesAny(user.Roles);
}

public sealed class RemoveTagFromMessageAuthorisation
{
    public bool Allows(SignedInUser user, RemoveTagFromMessage command) =>
        TriageRoles.AllowedToTriage.IncludesAny(user.Roles);
}

public sealed class CreateRequestFromMessageAuthorisation
{
    public bool Allows(SignedInUser user, CreateRequestFromMessage command) =>
        TriageRoles.AllowedToTriage.IncludesAny(user.Roles);
}

using Jewel.JPMS.Contracts.MailboxCompose;

namespace Jewel.JPMS.Api.Features.MailboxIntake.Compose;

/// <summary>
/// The gate class for the compose command, added 2026-09-04 so the connector's action gateway can
/// compose it (send_mailbox_email — the Control Centre's Reply box and Compose pane, mirrored).
/// <see cref="SendMailboxEmailEndpoint"/> keeps its inline check: both sides read the SAME RoleSet
/// constant (JpmsRoleSets.AllInternal, every internal role — decision 2026-08-10), so there is one
/// source of truth. Externals never pass.
/// </summary>
public sealed class SendMailboxEmailAuthorisation
{
    public bool Allows(SignedInUser user, SendMailboxEmail command) =>
        JpmsRoleSets.AllInternal.IncludesAny(user.Roles);
}

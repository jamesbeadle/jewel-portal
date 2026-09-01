using Jewel.JPMS.Contracts.ArchitectInstructions;

namespace Jewel.JPMS.Api.Features.ArchitectInstructions;

// Gate classes for the register's plain-JSON commands, added 2026-08-31 so the connector's action
// gateway can compose them (docs/ai/11 §4 — the AddWorker pattern). The endpoints keep their
// inline checks: both sides read the SAME RoleSet constant (ArchitectInstructionRoles.
// AllowedToManage), so there is one source of truth and swapping five endpoint bodies would add
// ceremony without safety. The multipart RecordArchitectInstruction stays endpoint-only.

public sealed class ImportArchitectInstructionFromMessageAuthorisation
{
    public bool Allows(SignedInUser user, ImportArchitectInstructionFromMessage command) =>
        ArchitectInstructionRoles.AllowedToManage.IncludesAny(user.Roles);
}

public sealed class UpdateArchitectInstructionAuthorisation
{
    public bool Allows(SignedInUser user, UpdateArchitectInstruction command) =>
        ArchitectInstructionRoles.AllowedToManage.IncludesAny(user.Roles);
}

public sealed class LinkArchitectInstructionToVariationAuthorisation
{
    public bool Allows(SignedInUser user, LinkArchitectInstructionToVariation command) =>
        ArchitectInstructionRoles.AllowedToManage.IncludesAny(user.Roles);
}

public sealed class UnlinkArchitectInstructionFromVariationAuthorisation
{
    public bool Allows(SignedInUser user, UnlinkArchitectInstructionFromVariation command) =>
        ArchitectInstructionRoles.AllowedToManage.IncludesAny(user.Roles);
}

public sealed class DeleteArchitectInstructionAuthorisation
{
    public bool Allows(SignedInUser user, DeleteArchitectInstruction command) =>
        ArchitectInstructionRoles.AllowedToManage.IncludesAny(user.Roles);
}

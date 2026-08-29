using Jewel.JPMS.Models;

namespace Jewel.JPMS.Api.Gates;

/// <summary>The authenticated caller. SubcontractorId is set only for external subcontractor
/// contacts (see DirectoryUserEntity.SubcontractorId) and scopes portal endpoints to their own
/// company's data. Roles is the EFFECTIVE list (a directory Admin role expands to every role);
/// HomeRole is the user's own role (HomeRoleSelection over the raw directory roles) and
/// RevertToOwnRole the per-user opt-in for the client's "Viewing as" switch defaulting back to
/// it — both ride along for the auth endpoints, no gate reads them.</summary>
public sealed record SignedInUser(
    string Email, string DisplayName, IReadOnlyList<Role> Roles, string? SubcontractorId = null,
    Role? HomeRole = null, bool RevertToOwnRole = false);

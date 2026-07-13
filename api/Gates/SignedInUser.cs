using Jewel.JPMS.Models;

namespace Jewel.JPMS.Api.Gates;

/// <summary>The authenticated caller. SubcontractorId is set only for external subcontractor
/// contacts (see DirectoryUserEntity.SubcontractorId) and scopes portal endpoints to their own
/// company's data.</summary>
public sealed record SignedInUser(
    string Email, string DisplayName, IReadOnlyList<Role> Roles, string? SubcontractorId = null);

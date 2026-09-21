namespace Jewel.JPMS.Models;

/// <summary>
/// Who may work with Architect's Instructions. Deliberately wider than the internal-only default:
/// the architect issues the instruction, so the architect can file it themselves rather than
/// emailing it and waiting for someone at Jewel to key it in. Everyone else on the list is a role
/// that already owns the commercial consequence of an instruction.
/// </summary>
public static class ArchitectInstructionRoles
{
    /// <summary>File, correct, link and delete instructions.</summary>
    public static readonly RoleSet AllowedToManage = RoleSet.Of(
        Role.Admin,
        JpmsRoles.Director,          // Managing Director
        JpmsRoles.FinanceDirector,
        JpmsRoles.ProjectManager,
        JpmsRoles.Architect);

    /// <summary>Read the register and download the documents — everyone who works from them.</summary>
    public static readonly RoleSet AllowedToRead = RoleSet.Of(
        Role.Admin,
        JpmsRoles.Director,
        JpmsRoles.FinanceDirector,
        JpmsRoles.ProjectManager,
        JpmsRoles.Estimator,         // Quantity Surveyor — prices the instructed work
        JpmsRoles.SiteManager,
        JpmsRoles.Foreman,
        JpmsRoles.Architect);
}

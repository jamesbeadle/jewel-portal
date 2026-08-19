using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Drawings;

namespace Jewel.JPMS.Api.Features.Drawings.Commands;

// Folder management is register curation, so the gate matches RegisterDrawingAuthorisation —
// the roles that may shape the register may also shape its folders.

public sealed class CreateDrawingFolderAuthorisation
{
    private static readonly RoleSet RolesThatMayManageFolders =
        RoleSet.Of(JpmsRoles.Director, JpmsRoles.ProjectManager, JpmsRoles.Estimator);

    public bool Allows(SignedInUser user, CreateDrawingFolder command) =>
        RolesThatMayManageFolders.IncludesAny(user.Roles);
}

public sealed class RenameDrawingFolderAuthorisation
{
    private static readonly RoleSet RolesThatMayManageFolders =
        RoleSet.Of(JpmsRoles.Director, JpmsRoles.ProjectManager, JpmsRoles.Estimator);

    public bool Allows(SignedInUser user, RenameDrawingFolder command) =>
        RolesThatMayManageFolders.IncludesAny(user.Roles);
}

public sealed class DeleteDrawingFolderAuthorisation
{
    private static readonly RoleSet RolesThatMayManageFolders =
        RoleSet.Of(JpmsRoles.Director, JpmsRoles.ProjectManager, JpmsRoles.Estimator);

    public bool Allows(SignedInUser user, DeleteDrawingFolder command) =>
        RolesThatMayManageFolders.IncludesAny(user.Roles);
}

public sealed class MoveDrawingToFolderAuthorisation
{
    private static readonly RoleSet RolesThatMayManageFolders =
        RoleSet.Of(JpmsRoles.Director, JpmsRoles.ProjectManager, JpmsRoles.Estimator);

    public bool Allows(SignedInUser user, MoveDrawingToFolder command) =>
        RolesThatMayManageFolders.IncludesAny(user.Roles);
}

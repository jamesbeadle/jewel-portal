using Jewel.JPMS.Contracts.DocumentControl;

namespace Jewel.JPMS.Api.Features.DocumentControl.Commands;

public sealed class FileDocumentAsDrawingAuthorisation
{
    public bool Allows(SignedInUser user, FileDocumentAsDrawing command) =>
        DocumentControlRoles.AllowedToManage.IncludesAny(user.Roles);
}

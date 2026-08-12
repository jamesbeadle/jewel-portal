using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.DocumentControl;

namespace Jewel.JPMS.Api.Features.DocumentControl.Commands;

public sealed class FileDocumentToSubcontractorAuthorisation
{
    public bool Allows(SignedInUser user, FileDocumentToSubcontractor command) =>
        DocumentControlRoles.AllowedToManage.IncludesAny(user.Roles);
}

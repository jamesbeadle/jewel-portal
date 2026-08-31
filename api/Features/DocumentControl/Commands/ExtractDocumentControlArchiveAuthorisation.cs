using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.DocumentControl;

namespace Jewel.JPMS.Api.Features.DocumentControl.Commands;

public sealed class ExtractDocumentControlArchiveAuthorisation
{
    public bool Allows(SignedInUser user, ExtractDocumentControlArchive command) =>
        DocumentControlRoles.AllowedToManage.IncludesAny(user.Roles);
}

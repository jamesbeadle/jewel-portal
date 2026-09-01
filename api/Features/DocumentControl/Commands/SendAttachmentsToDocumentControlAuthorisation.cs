using Jewel.JPMS.Contracts.DocumentControl;

namespace Jewel.JPMS.Api.Features.DocumentControl.Commands;

public sealed class SendAttachmentsToDocumentControlAuthorisation
{
    public bool Allows(SignedInUser user, SendAttachmentsToDocumentControl command) =>
        DocumentControlRoles.AllowedToManage.IncludesAny(user.Roles);
}

using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.DocumentControl;

namespace Jewel.JPMS.Api.Features.DocumentControl.Commands;

public sealed class FileDocumentAsPaymentCertificateAuthorisation
{
    public bool Allows(SignedInUser user, FileDocumentAsPaymentCertificate command) =>
        DocumentControlRoles.AllowedToManage.IncludesAny(user.Roles);
}

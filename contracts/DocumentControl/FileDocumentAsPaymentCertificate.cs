using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.DocumentControl;

// Files a pending Document Control item as a payment certificate on a project: the certificate
// gets its own blob copy (deleting nothing in Document Control can ever orphan it) and appears in
// the Finance → Payment Certificates register. ValuationClaimId optionally ties it to the claim it
// certifies. Returns the item, now Filed.
public sealed record FileDocumentAsPaymentCertificate(
    string DocumentControlItemId,
    string ProjectId,
    string CertificateNumber,
    decimal? CertifiedAmount,
    DateTimeOffset IssuedDate,
    string? ValuationClaimId) : ICommand<DocumentControlItem>;

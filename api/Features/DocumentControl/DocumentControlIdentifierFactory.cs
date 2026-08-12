namespace Jewel.JPMS.Api.Features.DocumentControl;

internal static class DocumentControlIdentifierFactory
{
    private const string CompactGuidFormat = "N";

    public static string NextDocumentControlItemId() => Guid.NewGuid().ToString(CompactGuidFormat);
    public static string NextPaymentCertificateId() => Guid.NewGuid().ToString(CompactGuidFormat);
}

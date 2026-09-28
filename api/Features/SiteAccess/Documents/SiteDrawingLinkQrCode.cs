using QRCoder;

namespace Jewel.JPMS.Api.Features.SiteAccess.Documents;

/// <summary>The QR code for a site link's URL as PNG bytes, from the pure-managed generator that
/// runs on the Linux Functions host.</summary>
public static class SiteDrawingLinkQrCode
{
    private const int PixelsPerModule = 12;

    public static byte[] Png(string url)
    {
        using var generator = new QRCodeGenerator();
        using var data = generator.CreateQrCode(url, QRCodeGenerator.ECCLevel.M);
        using var code = new PngByteQRCode(data);
        return code.GetGraphic(PixelsPerModule);
    }
}

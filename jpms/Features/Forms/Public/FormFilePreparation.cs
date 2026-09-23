using Jewel.JPMS.Contracts.Forms;

namespace Jewel.JPMS.Features.Forms.Public;

public sealed record PreparedFile(string FileName, string Base64);

/// <summary>
/// A chosen file made ready to leave the phone, as the dashboard's page made it: a photograph of
/// 1.5 MB or more is shrunk to 1600 pixels as a JPEG (a phone photo is 3–8 MB, the shrunk one a few
/// hundred KB), anything else goes as it is, and what is still too large after that is refused on
/// the page with a sentence rather than failing on the way.
/// </summary>
public static class FormFilePreparation
{
    private const string Jpeg = "image/jpeg";
    private const string ImagePrefix = "image/";
    private const string JpegExtension = ".jpg";

    public static async Task<(PreparedFile? File, string? Problem)> PrepareAsync(IBrowserFile chosen)
    {
        var prepared = await ReadAsync(chosen);
        var isTooLarge = prepared.Base64.Length > PublicFormLimits.LongestEncodedOnThePage;
        return isTooLarge ? (null, FormWording.TooLargeToSend(prepared.FileName)) : (prepared, null);
    }

    private static async Task<PreparedFile> ReadAsync(IBrowserFile chosen)
    {
        var isALargePhoto = chosen.ContentType.StartsWith(ImagePrefix, StringComparison.OrdinalIgnoreCase)
            && chosen.Size >= PublicFormLimits.ShrinkPhotosOver;
        var shrunk = isALargePhoto ? await ShrunkAsync(chosen) : null;
        return shrunk ?? new PreparedFile(chosen.Name, await Base64Of(chosen));
    }

    private static async Task<PreparedFile?> ShrunkAsync(IBrowserFile chosen)
    {
        try
        {
            var edge = PublicFormLimits.LongestPhotoEdge;
            var photo = await chosen.RequestImageFileAsync(Jpeg, edge, edge);
            return new PreparedFile(Path.GetFileNameWithoutExtension(chosen.Name) + JpegExtension, await Base64Of(photo));
        }
        catch (JSException) { return null; }
    }

    private static async Task<string> Base64Of(IBrowserFile file)
    {
        await using var stream = file.OpenReadStream(PublicFormLimits.LargestFileChosen);
        using var buffer = new MemoryStream();
        await stream.CopyToAsync(buffer);
        return Convert.ToBase64String(buffer.ToArray());
    }
}

using Microsoft.AspNetCore.Components.Forms;

namespace Jewel.JPMS.Features.Progress;

/// <summary>A photograph read off the phone the moment it is chosen. The browser hands a form a
/// fresh file list on every Choose, and the files of the last list can no longer be read — so a
/// photograph taken with the camera, one per Choose, is kept as its bytes until the day is saved.</summary>
public sealed record ChosenPhoto(string Name, string ContentType, DateTimeOffset LastModified, byte[] Bytes)
{
    private const long MaxPhotoBytes = 100L * 1024 * 1024;

    public long Size => Bytes.LongLength;

    public static async Task<ChosenPhoto> ReadAsync(IBrowserFile file, CancellationToken cancellationToken)
    {
        await using var stream = file.OpenReadStream(MaxPhotoBytes, cancellationToken);
        using var buffer = new MemoryStream();
        await stream.CopyToAsync(buffer, cancellationToken);
        return new ChosenPhoto(file.Name, file.ContentType, file.LastModified, buffer.ToArray());
    }
}

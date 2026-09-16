namespace Jewel.JPMS.Api.Features.Progress.Photos;

/// <summary>An image as it arrived — from a form, a mailbox attachment or a WhatsApp zip — before
/// the intake has looked at it.</summary>
public sealed record IncomingProgressPhoto(string FileName, string? ContentType, byte[] Bytes);

/// <summary>An image ready to store: converted to a browser format, turned the right way up, shrunk
/// to display size, and hashed on the bytes it ARRIVED as, so the same original posted twice is
/// recognised whatever it was re-encoded to.</summary>
public sealed record PreparedProgressPhoto(
    string FileName,
    string ContentType,
    byte[] Bytes,
    string ContentHash);

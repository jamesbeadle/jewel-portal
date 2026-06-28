namespace Jewel.JPMS.Models;

/// <summary>
/// A rendered request (RFI etc.) document, ready to stream to the caller. The bytes are regenerated
/// from the SQL source of truth on every request, so the file is always current and nothing is stored.
/// </summary>
public sealed record RequestDocumentFile(string FileName, string ContentType, byte[] Content);

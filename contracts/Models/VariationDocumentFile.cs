namespace Jewel.JPMS.Models;

/// <summary>
/// A rendered variation order document, ready to stream to the caller. The bytes are regenerated
/// from the SQL source of truth on every request — download, email attach, resend — so the file is
/// always the record as it currently stands and nothing is stored. (Same arrangement as
/// <see cref="RequestDocumentFile"/>.)
/// </summary>
public sealed record VariationDocumentFile(string FileName, string ContentType, byte[] Content);

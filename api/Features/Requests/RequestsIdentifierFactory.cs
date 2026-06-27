namespace Jewel.JPMS.Api.Features.Requests;

internal static class RequestsIdentifierFactory
{
    private const string CompactGuidFormat = "N";

    /// <summary>Prefix for the human-readable request number / mailbox folder name.</summary>
    public const string NumberPrefix = "REQ-";

    public static string Next() => Guid.NewGuid().ToString(CompactGuidFormat);

    /// <summary>
    /// The mailbox folder name (and display number) for a request, e.g. 1 => "REQ-0001".
    /// Shared by the API (display) and the worker (folder creation) so they always agree.
    /// </summary>
    public static string FolderName(int number) => $"{NumberPrefix}{number:0000}";
}

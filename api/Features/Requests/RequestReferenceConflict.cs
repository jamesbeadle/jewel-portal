using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Requests;

/// <summary>
/// Classifies save failures caused by the per-project reference unique index
/// (UX_Requests_Project_Reference). The <see cref="RequestReferenceGuard"/> catches most duplicates
/// up-front with a friendly message, but it is check-then-act and can race; the index is the real
/// guarantee. Handlers use this to turn the raw database violation into the same outcome the guard
/// would have produced: an auto-minted reference is re-minted and retried, a manually typed one is
/// rejected with the guard's message.
/// </summary>
internal static class RequestReferenceConflict
{
    public const string IndexName = "UX_Requests_Project_Reference";

    /// <summary>True when the failed save was rejected by the per-project reference unique index.</summary>
    public static bool IsReferenceClash(DbUpdateException exception) =>
        exception.InnerException?.Message.Contains(IndexName, StringComparison.OrdinalIgnoreCase) == true;

    /// <summary>The same human-readable rejection the guard raises, for parity between both paths.</summary>
    public static InvalidOperationException AsFriendlyError(string reference) =>
        new($"Reference '{reference.Trim()}' is already used by another request on this project. Choose a different number.");
}

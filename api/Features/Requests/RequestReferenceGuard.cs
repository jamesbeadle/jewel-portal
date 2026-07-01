using Jewel.JPMS.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Requests;

/// <summary>
/// Enforces that a request's human reference (e.g. "RFI-049") is unique within its project. This backs
/// the manual-override path: a user may type any number they like, but they can never save one that is
/// already in use — the save is rejected rather than duplicating or silently overwriting another record.
/// </summary>
internal static class RequestReferenceGuard
{
    /// <summary>
    /// Throws <see cref="InvalidOperationException"/> when another request on <paramref name="projectId"/>
    /// already carries <paramref name="reference"/> (compared case-insensitively). Pass the request's own
    /// id as <paramref name="excludeRequestId"/> on an update so it doesn't clash with itself; pass null
    /// when creating. Blank references are left to the field validators, which already require one.
    /// </summary>
    public static async Task EnsureUniqueAsync(
        JpmsContext context,
        string projectId,
        string reference,
        string? excludeRequestId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(reference)) return;

        var normalised = reference.Trim().ToUpperInvariant();
        var clash = await context.Requests.AnyAsync(
            r => r.ProjectId == projectId
                && r.RequestId != excludeRequestId
                && r.Reference.ToUpper() == normalised,
            cancellationToken);

        if (clash)
            throw new InvalidOperationException(
                $"Reference '{reference.Trim()}' is already used by another request on this project. Choose a different number.");
    }
}

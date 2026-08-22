using Jewel.JPMS.Api.Features.Audit;
using Jewel.JPMS.Api.Features.MailboxIntake.Graph;

namespace Jewel.JPMS.Api.Features.RecordLinks;

/// <summary>
/// The cross-filing confirm, checked BEFORE a record is created from an email.
///
/// <para>The create-from-message commands (work order, bid package, defect, request) persist the
/// record first and link the email after — so when the link path's cross-pathway rejection fired,
/// the record already existed: the Control Centre showed a red error with no confirm button, and a
/// retry created a DUPLICATE record (the 2026-08-22 "WO-CA63BC67" glitch). This guard runs the
/// SAME check against the email's current categories before anything persists, throwing the same
/// "Confirm the cross-filing" wording the UI already recognises (IsCrossFilePrompt → the amber
/// "File under both anyway" retry, which re-runs the command with AllowCrossPathway). A rejection
/// here costs nothing — no record, no tag, nothing to duplicate.</para>
/// </summary>
public static class CrossPathwayGuard
{
    /// <summary>
    /// Throws the standard cross-filing confirm when filing <paramref name="bucket"/> would put the
    /// thread under a second pathway and <paramref name="allowCrossPathway"/> has not been given.
    /// <paramref name="newRecordLabel"/> names what is being created ("the new work order") — there
    /// is no reference to show yet, which is exactly the point of pre-flighting.
    /// </summary>
    public static void EnsureConfirmed(
        IEnumerable<string>? categories, string? bucket, bool allowCrossPathway, string newRecordLabel)
    {
        if (bucket is null || allowCrossPathway) return;
        var existing = (categories ?? Array.Empty<string>())
            .Where(TriageCategories.IsBucketTag)
            .FirstOrDefault(existingBucket => !existingBucket.Equals(bucket, StringComparison.OrdinalIgnoreCase));
        if (existing is null) return;
        throw new InvalidOperationException(
            $"This thread is filed under {AuditTrail.PathwayLabel(existing)}; {newRecordLabel} would also file it under {AuditTrail.PathwayLabel(bucket)}. "
            + "Confirm the cross-filing to proceed, or link the email to a record on the same pathway.");
    }
}

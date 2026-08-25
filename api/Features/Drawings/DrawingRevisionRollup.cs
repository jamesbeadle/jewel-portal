using Jewel.JPMS.Models;

namespace Jewel.JPMS.Api.Features.Drawings;

/// <summary>
/// What the register needs to know about a drawing's revisions without loading them: counts by
/// status, whether one is Approved, and the latest revision's file name and pipeline stamps.
/// </summary>
internal sealed record DrawingRevisionRollup(
    int UnapprovedCount,
    int ArchivedCount,
    bool HasApprovedRevision,
    string? LatestFileName,
    DateTimeOffset? LatestMetadataExtractedAt,
    DateTimeOffset? LatestAnalysedAt)
{
    /// <summary>The slice of a revision row the rollup reads — projected in the query so the
    /// blob references and stamps of every revision are never pulled just to count them.</summary>
    internal sealed record RevisionSummary(
        string DrawingId,
        int ApprovalStatus,
        DateTimeOffset ReceivedAt,
        string FileName,
        DateTimeOffset? MetadataExtractedAt,
        DateTimeOffset? AnalysedAt);

    public static readonly DrawingRevisionRollup None = new(0, 0, false, null, null, null);

    public static DrawingRevisionRollup Of(IReadOnlyCollection<RevisionSummary> revisions)
    {
        if (revisions.Count == 0) return None;
        var unapproved = revisions.Count(revision => revision.ApprovalStatus == (int)DrawingApprovalStatus.Unapproved);
        var archived = revisions.Count(revision => revision.ApprovalStatus == (int)DrawingApprovalStatus.Archived);
        var hasApproved = revisions.Any(revision => revision.ApprovalStatus == (int)DrawingApprovalStatus.Approved);
        // The latest revision is the one the register describes: its file name and whether the
        // newest issue has been extracted and analysed yet.
        var latest = revisions.OrderByDescending(revision => revision.ReceivedAt).First();
        return new DrawingRevisionRollup(
            unapproved, archived, hasApproved, latest.FileName, latest.MetadataExtractedAt, latest.AnalysedAt);
    }
}

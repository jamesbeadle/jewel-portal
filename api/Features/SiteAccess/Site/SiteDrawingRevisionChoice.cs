using Jewel.JPMS.Api.Data.Entities;

namespace Jewel.JPMS.Api.Features.SiteAccess.Site;

/// <summary>Which revision a scan shows: the approved one when the drawing has one; otherwise the
/// newest received, shown as not approved rather than passed off as issued. Only a revision with
/// a stored file can be shown at all.</summary>
public static class SiteDrawingRevisionChoice
{
    public static DrawingRevisionEntity? Shown(IEnumerable<DrawingRevisionEntity> revisions)
    {
        var stored = revisions.Where(HasStoredFile).ToList();
        var approved = stored.FirstOrDefault(IsApproved);
        return approved ?? stored.OrderByDescending(revision => revision.ReceivedAt).FirstOrDefault();
    }

    public static bool IsApproved(DrawingRevisionEntity revision) =>
        revision.ApprovalStatus == (int)DrawingApprovalStatus.Approved;

    private static bool HasStoredFile(DrawingRevisionEntity revision) =>
        !string.IsNullOrWhiteSpace(revision.BlobRef);
}

namespace Jewel.JPMS.Models;

/// <summary>
/// Approval lifecycle of a single drawing revision.
/// A revision is uploaded <see cref="Unapproved"/>; approving it makes it <see cref="Approved"/>
/// (the one canonical version) and archives every other revision of the same drawing.
/// </summary>
public enum DrawingApprovalStatus
{
    Unapproved = 0,
    Approved = 1,
    Archived = 2
}

/// <summary>
/// Filter for listing a drawing's revisions by approval status.
/// </summary>
public enum DrawingRevisionStatusFilter
{
    All = 0,
    Approved = 1,
    Unapproved = 2,
    Archived = 3
}

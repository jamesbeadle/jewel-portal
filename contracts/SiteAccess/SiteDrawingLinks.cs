using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.SiteAccess;

/// <summary>A project's site drawing links, newest first, active and spent alike — what the
/// register's Site links panel reads.</summary>
public sealed record ListSiteDrawingLinksForProject(string ProjectId)
    : IQuery<IReadOnlyList<SiteDrawingLink>>;

/// <summary>
/// Mints a link for one folder of the project's document register (and the folders beneath it
/// when <paramref name="IncludeSubFolders"/>), labelled as the poster will read and expiring after
/// <paramref name="ExpiresInDays"/>. The endpoint stamps the signed-in user as
/// <paramref name="CreatedByEmail"/> and the public site as <paramref name="SiteOrigin"/>; neither
/// is the caller's to choose. The answer carries the raw URL, the QR code and the poster exactly once.
/// </summary>
public sealed record CreateSiteDrawingLink(
    string ProjectId,
    string DrawingFolderId,
    string Label,
    bool IncludeSubFolders = true,
    int ExpiresInDays = SiteDrawingLinkLimits.DefaultExpiryDays,
    string CreatedByEmail = "",
    string SiteOrigin = "") : ICommand<SiteDrawingLinkCreated>;

/// <summary>Stops a link at once: from the next scan the same QR answers a plain 404. A revoked
/// link stays on the register with its scan count; it is never deleted and never re-armed.</summary>
public sealed record RevokeSiteDrawingLink(
    string SiteDrawingLinkId,
    string RevokedByEmail = "") : ICommand<Acknowledgement>;

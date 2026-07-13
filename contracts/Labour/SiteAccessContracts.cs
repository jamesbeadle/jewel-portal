using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Labour;

/// <summary>Returns the project's active site token, creating one if none exists yet.</summary>
public sealed record GetSiteAccess(string ProjectId) : IQuery<SiteAccess>;

/// <summary>Deactivates the current token and issues a fresh one — old QR codes stop working
/// immediately. Use if a QR code leaks or a printed sheet goes missing.</summary>
public sealed record RotateSiteAccessToken(string ProjectId) : ICommand<SiteAccess>;

public sealed record ListSiteAttendanceForProject(string ProjectId)
    : IQuery<IReadOnlyList<SiteAttendance>>;

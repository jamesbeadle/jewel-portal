using Jewel.JPMS.Contracts.Cqrs;

namespace Jewel.JPMS.Contracts.Platform;

/// <summary>The version the system is currently announcing to every signed-in tab, and the audit
/// of who published it. Distinct from BuildVersion (the number compiled into a bundle): this one
/// lives in the database and moves only when an administrator publishes an update from
/// Admin → System — that is what raises the UpdateToast on every open tab.</summary>
public sealed record AnnouncedAppVersion(long Version, DateTimeOffset PublishedAt, string PublishedBy);

/// <summary>The announced version with its publish audit — what Admin → System shows.</summary>
public sealed record GetAnnouncedAppVersion() : IQuery<AnnouncedAppVersion>;

/// <summary>Bumps the announced version by one, prompting every open tab to refresh. Deliberately
/// carries no target number — one click, one increment, nothing to mistype and no way to move the
/// number backwards. PublishedBy is stamped server-side from the signed-in administrator — the
/// client sends nothing.</summary>
public sealed record PublishAppVersion(string PublishedBy = "") : ICommand<AnnouncedAppVersion>;

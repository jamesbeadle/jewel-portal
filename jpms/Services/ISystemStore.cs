using Jewel.JPMS.Contracts.Platform;

namespace Jewel.JPMS.Services;

/// <summary>The announced app version behind Admin → System: the version every signed-in tab is
/// being asked to run, and the act of moving it.</summary>
public interface ISystemStore
{
    /// <summary>Null until the first fetch lands — the only honest "not fetched yet".</summary>
    AnnouncedAppVersion? Current { get; }

    bool IsLoaded { get; }

    event Action? OnChange;

    Task RefreshAsync(CancellationToken cancellationToken);

    /// <summary>Bumps the announced version by one; every open tab is then offered the refresh.</summary>
    Task PublishAsync(CancellationToken cancellationToken);
}

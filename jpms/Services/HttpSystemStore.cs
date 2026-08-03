using Jewel.JPMS.Contracts.Platform;
using Jewel.JPMS.Cqrs;

namespace Jewel.JPMS.Services;

public sealed class HttpSystemStore : ISystemStore
{
    private readonly IQueryClient queries;
    private readonly ICommandSender commands;
    private readonly AppVersionService versions;

    public HttpSystemStore(IQueryClient queries, ICommandSender commands, AppVersionService versions)
    {
        this.queries = queries;
        this.commands = commands;
        this.versions = versions;
    }

    public AnnouncedAppVersion? Current { get; private set; }

    public bool IsLoaded => Current is not null;

    public event Action? OnChange;

    public async Task RefreshAsync(CancellationToken cancellationToken)
    {
        Current = await queries.AskAsync(new GetAnnouncedAppVersion(), cancellationToken);
        OnChange?.Invoke();
    }

    public async Task PublishAsync(CancellationToken cancellationToken)
    {
        // The command returns the row it wrote, so the page shows the new number without a second
        // round trip. PublishedBy is stamped server-side from the signed-in administrator.
        Current = await commands.SendAsync(new PublishAppVersion(), cancellationToken);

        // The publish response's own header still carried the OLD number (the middleware stamps
        // before the handler runs), so hand the new one to the watcher directly: the admin's own
        // tab raises the UpdateToast immediately — living proof the publish worked.
        versions.Observe(Current.Version.ToString());
        OnChange?.Invoke();
    }
}

using Jewel.JPMS.Contracts.Bluebeam;

namespace Jewel.JPMS.Services;

/// <summary>
/// The shared Bluebeam connection's status, fetched once and kept for the session
/// (stale-while-revalidate: pages paint what's held and refresh behind it). The drawing pages read
/// it to enable or disable their Extract buttons; Admin → Integrations refreshes it after every
/// connect/disconnect. Connection state changes rarely, so staleness here costs a tooltip at worst.
/// </summary>
public sealed class BluebeamStatusStore
{
    private readonly IQueryClient queries;
    private readonly ICommandSender commands;

    public BluebeamStatusStore(IQueryClient queries, ICommandSender commands)
    {
        this.queries = queries;
        this.commands = commands;
    }

    public event Action? OnChange;

    public bool IsLoaded { get; private set; }
    public BluebeamStatus? Current { get; private set; }

    public bool IsConnected => Current is { IsConfigured: true, Connected: true };

    public async Task RefreshAsync(CancellationToken cancellationToken = default)
    {
        Current = await queries.AskAsync(new GetBluebeamStatus(), cancellationToken);
        IsLoaded = true;
        OnChange?.Invoke();
    }

    /// <summary>Loads once per session; pages that only need the buttons enabled call this and
    /// carry on — a failure leaves IsLoaded false and the buttons conservatively disabled.</summary>
    public async Task EnsureLoadedAsync(CancellationToken cancellationToken = default)
    {
        if (IsLoaded) return;
        try { await RefreshAsync(cancellationToken); }
        catch { /* reported by the query client; buttons stay disabled */ }
    }

    public async Task<BluebeamConnectStart> StartConnectAsync(CancellationToken cancellationToken = default) =>
        await commands.SendAsync(new StartBluebeamConnect(), cancellationToken);

    public async Task DisconnectAsync(CancellationToken cancellationToken = default)
    {
        Current = await commands.SendAsync(new DisconnectBluebeam(), cancellationToken);
        IsLoaded = true;
        OnChange?.Invoke();
    }
}

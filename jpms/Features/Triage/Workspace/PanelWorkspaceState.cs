namespace Jewel.JPMS.Features.Triage.Workspace;

/// <summary>
/// What the two workspace panes are showing, and where everything was left. Each pane keeps a
/// history of the kinds it has shown, so closing a preview (or losing a kind to the other pane)
/// falls back to what that pane showed before. On mobile only the left pane is visible, so every
/// show lands there. The page owns one instance and re-renders on <see cref="OnChange"/>.
/// </summary>
public sealed partial class PanelWorkspaceState
{
    public event Action? OnChange;

    private readonly List<PanelKind> leftHistory = new() { PanelKind.Inbox };
    private readonly List<PanelKind> rightHistory = new() { PanelKind.Email };
    private readonly HashSet<PanelKind> everShown = new() { PanelKind.Inbox, PanelKind.Email };

    /// <summary>The document the Preview pane is showing (null = no preview open).</summary>
    public PreviewRequest? Preview { get; private set; }

    public PanelKind ActiveOn(PanelSide side) => HistoryOf(side)[^1];

    /// <summary>True once a kind has been shown — content renders lazily, then stays alive.</summary>
    public bool HasShown(PanelKind kind) => everShown.Contains(kind);

    public PanelSide? SideShowing(PanelKind kind)
    {
        if (ActiveOn(PanelSide.Left) == kind) return PanelSide.Left;
        if (ActiveOn(PanelSide.Right) == kind) return PanelSide.Right;
        return null;
    }

    public void Show(PanelKind kind, PanelSide side)
    {
        if (!IsDesktop) side = PanelSide.Left;
        // The email is the one kind both windows can hold at once: asking for it while the other
        // pane already shows it opens the read-only mirror HERE instead of stealing the email (and
        // a half-written reply) across — the original stays put, this side gets a copy to read.
        if (kind == PanelKind.Email && IsDesktop && ActiveOn(OtherThan(side)) == PanelKind.Email)
            kind = PanelKind.EmailMirror;
        if (ActiveOn(side) == kind) return;
        if (ActiveOn(OtherThan(side)) == kind) FallBack(OtherThan(side), kind);
        var history = HistoryOf(side);
        history.Remove(kind);
        history.Add(kind);
        // The real email arriving on a side makes any mirror queued beneath it pointless.
        if (kind == PanelKind.Email) history.Remove(PanelKind.EmailMirror);
        everShown.Add(kind);
        Notify();
    }

    /// <summary>Show a kind beside its anchor — an email opens opposite the list that was clicked.</summary>
    public void ShowOpposite(PanelKind kind, PanelKind anchor) =>
        Show(kind, OtherThan(SideShowing(anchor) ?? PanelSide.Left));

    /// <summary>Take a kind off whichever pane shows it; that pane returns to what it showed before.</summary>
    public void Close(PanelKind kind)
    {
        if (SideShowing(kind) is not { } side) return;
        FallBack(side, kind);
        Notify();
    }

    public void OpenPreview(PreviewRequest document, PanelKind anchor)
    {
        Preview = document;
        ShowOpposite(PanelKind.Preview, anchor);
        Notify();
    }

    public void ClosePreview()
    {
        Preview = null;
        if (SideShowing(PanelKind.Preview) is { } side) FallBack(side, PanelKind.Preview);
        // Scrub any deeper history entry too — with no document there is nothing to fall back TO.
        leftHistory.Remove(PanelKind.Preview);
        rightHistory.Remove(PanelKind.Preview);
        Notify();
    }

    private static PanelSide OtherThan(PanelSide side) => side == PanelSide.Left ? PanelSide.Right : PanelSide.Left;

    private List<PanelKind> HistoryOf(PanelSide side) => side == PanelSide.Left ? leftHistory : rightHistory;

    // Drop the kind from a pane's history so the previous distinct kind resurfaces — skipping
    // whatever the other pane is showing. A pane can never end up empty: it reseeds with its home
    // kind (Inbox left, Email right), or the record explorer if that home is on show opposite.
    private void FallBack(PanelSide side, PanelKind leaving)
    {
        var history = HistoryOf(side);
        var otherActive = ActiveOn(OtherThan(side));
        history.Remove(leaving);
        while (history.Count > 0 && history[^1] == otherActive) history.RemoveAt(history.Count - 1);
        if (history.Count > 0) return;
        var homeKind = side == PanelSide.Left ? PanelKind.Inbox : PanelKind.Email;
        history.Add(homeKind == otherActive ? PanelKind.Records : homeKind);
    }

    internal void Notify() => OnChange?.Invoke();
}

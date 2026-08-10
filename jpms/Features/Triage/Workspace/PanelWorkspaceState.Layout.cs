namespace Jewel.JPMS.Features.Triage.Workspace;

/// <summary>
/// The workspace's physical layout half: where the divider sits and whether the viewport is
/// desktop (two panes) or mobile (the left pane alone). Split from the pane-content half so each
/// file reads as one concern.
/// </summary>
public sealed partial class PanelWorkspaceState
{
    /// <summary>Left pane's share of the width — the email list's third by default.</summary>
    public double LeftFraction { get; private set; } = 1.0 / 3.0;

    public bool IsDesktop { get; private set; } = true;

    /// <summary>The divider was dragged (reported by panel-workspace.js on release).</summary>
    public void SetLeftFraction(double fraction)
    {
        LeftFraction = Math.Clamp(fraction, 0.2, 0.8);
        Notify();
    }

    /// <summary>The viewport crossed the lg breakpoint (reported by panel-workspace.js).</summary>
    public void SetIsDesktop(bool isDesktop)
    {
        if (IsDesktop == isDesktop) return;
        IsDesktop = isDesktop;
        Notify();
    }

    /// <summary>Two panes side by side (the default), or one full-width pane — what a pane leaves
    /// behind when it pops out into its own browser window, and how a popout window starts.</summary>
    public bool IsSplit { get; private set; } = true;

    /// <summary>Collapse to one full-width pane, keeping the given side's content — the other
    /// side's just popped out into its own window. Its history survives for RestoreSplit.</summary>
    public void Solo(PanelSide keep)
    {
        if (!IsSplit) return;
        if (keep == PanelSide.Right) Show(ActiveOn(PanelSide.Right), PanelSide.Left);
        IsSplit = false;
        Notify();
    }

    /// <summary>Bring the second pane back — it resumes whatever its history holds.</summary>
    public void RestoreSplit()
    {
        if (IsSplit) return;
        IsSplit = true;
        Notify();
    }

    /// <summary>A popout window's boot: one full-width pane on the popped-out kind.</summary>
    public void StartSolo(PanelKind kind)
    {
        Show(kind, PanelSide.Left);
        IsSplit = false;
        Notify();
    }
}

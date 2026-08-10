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
}

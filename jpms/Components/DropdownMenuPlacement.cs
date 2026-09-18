namespace Jewel.JPMS.Components;

/// <summary>The toggle's rect as the browser reads it, with the viewport it was read against.</summary>
public sealed record TogglePlacement(
    double Top, double Left, double Right, double Bottom, double Width,
    double ViewportWidth, double ViewportHeight);

/// <summary>Where a dropdown's panel sits once it is drawn against the viewport instead of inside
/// its toggle's box. A panel that hangs inside the box is clipped by any scrolling ancestor — a
/// long register, a modal body — and taking the clipping off the ancestor instead loses the
/// reader's place in it. The browser does the reading; every decision about the result is here,
/// so it can be read without a browser.</summary>
public static class DropdownMenuPlacement
{
    private const double GapFromToggle = 4;
    private const double RoomAPanelWants = 240;

    public static string StyleFor(TogglePlacement toggle, bool alignsLeft, bool matchesToggleWidth) =>
        "position:fixed;" + HorizontalEdge(toggle, alignsLeft) + Width(toggle, matchesToggleWidth) + VerticalEdge(toggle);

    /// <summary>Pins the edge the panel hangs from, so it stays put against the toggle rather than
    /// against the page: the right edge by default, so a menu at the end of a bar cannot run off.</summary>
    private static string HorizontalEdge(TogglePlacement toggle, bool alignsLeft) =>
        alignsLeft
            ? FormattableString.Invariant($"left:{Math.Round(toggle.Left)}px;")
            : FormattableString.Invariant($"right:{Math.Round(toggle.ViewportWidth - toggle.Right)}px;");

    /// <summary>A panel that spanned its toggle by stretching inside it — the nav column's project
    /// picker — has no box to stretch in once it is fixed, so it is given the toggle's width.</summary>
    private static string Width(TogglePlacement toggle, bool matchesToggleWidth) =>
        matchesToggleWidth ? FormattableString.Invariant($"width:{Math.Round(toggle.Width)}px;") : "";

    /// <summary>Below the toggle, unless the panel would not fit there and there is more room above.</summary>
    private static string VerticalEdge(TogglePlacement toggle)
    {
        var roomBelow = toggle.ViewportHeight - toggle.Bottom;
        if (roomBelow >= RoomAPanelWants || roomBelow >= toggle.Top)
            return FormattableString.Invariant($"top:{Math.Round(toggle.Bottom + GapFromToggle)}px;");
        return FormattableString.Invariant($"bottom:{Math.Round(toggle.ViewportHeight - toggle.Top + GapFromToggle)}px;");
    }
}

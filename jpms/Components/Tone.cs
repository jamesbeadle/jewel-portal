namespace Jewel.JPMS.Components;

/// <summary>
/// The one status/feedback colour vocabulary, shared by <see cref="Notice"/> and <see cref="Pill"/>
/// (and anything else that colours by meaning). Maps 1:1 onto the Figma's Status styles — Positive,
/// Negative, Neutral (`info`) — plus Warning (a jpms addition, `warning` token) and Muted (no colour:
/// a fact, not a verdict). A view never picks a colour for a status; it picks a Tone and the
/// component decides.
/// </summary>
public enum Tone
{
    /// <summary>Nothing to react to — a plain fact ("Draft", "3 items").</summary>
    Muted,
    /// <summary>Neutral / informational — the Figma's Status/Neutral blue.</summary>
    Info,
    /// <summary>Done, approved, healthy — the Figma's Status/Positive green.</summary>
    Positive,
    /// <summary>Needs a look but nothing is broken — amber.</summary>
    Warning,
    /// <summary>Failed, rejected, overdue — the Figma's Status/Negative red.</summary>
    Negative
}

public static class ToneClasses
{
    /// <summary>Fill + border + text for a boxed message (Notice).</summary>
    public static string Box(this Tone tone) => tone switch
    {
        Tone.Negative => "bg-negative/10 border-negative/30 text-negative",
        Tone.Warning => "bg-warning/10 border-warning/30 text-warning",
        Tone.Positive => "bg-positive/10 border-positive/30 text-positive",
        Tone.Info => "bg-info/10 border-info/30 text-info",
        _ => "bg-surface-raised border-line text-content-muted"
    };

    /// <summary>Fill + text for a small badge (Pill).</summary>
    public static string Badge(this Tone tone) => tone switch
    {
        Tone.Negative => "bg-negative/10 text-negative",
        Tone.Warning => "bg-warning/10 text-warning",
        Tone.Positive => "bg-positive/10 text-positive",
        Tone.Info => "bg-info/10 text-info",
        _ => "bg-surface-raised text-content-subtle"
    };

    /// <summary>The 6px status dot's fill.</summary>
    public static string Dot(this Tone tone) => tone switch
    {
        Tone.Negative => "bg-negative",
        Tone.Warning => "bg-warning",
        Tone.Positive => "bg-positive",
        Tone.Info => "bg-info",
        _ => "bg-content-faint"
    };

    /// <summary>Text only.</summary>
    public static string Text(this Tone tone) => tone switch
    {
        Tone.Negative => "text-negative",
        Tone.Warning => "text-warning",
        Tone.Positive => "text-positive",
        Tone.Info => "text-info",
        _ => "text-content-subtle"
    };
}

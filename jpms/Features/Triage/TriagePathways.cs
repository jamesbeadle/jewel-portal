namespace Jewel.JPMS.Features.Triage;

/// <summary>
/// The communication pathway — WHO the correspondence is with (docs/Pathway-Split-Platform-Flow-
/// Plan.md §2). Triage is pathway-first: the pathway is chosen (or already fixed on the thread)
/// before any action, and it decides which action tabs and record types are offered.
/// Enum names double as the user-facing labels AND the short pathway strings the server's
/// commands accept ("Client" / "Subcontractor" / "Internal").
/// </summary>
public enum TriagePathway { Client, Subcontractor, Supplier, Internal }

/// <summary>One place for the pathway's names: bucket tags on the mailbox, labels and chip colours in the UI.</summary>
public static class TriagePathways
{
    public const string ClientBucket = "JPMS/Client";
    public const string SubcontractorBucket = "JPMS/Subcontractor";
    public const string SupplierBucket = "JPMS/Supplier";
    public const string InternalBucket = "JPMS/Internal";

    public static readonly TriagePathway[] All =
    {
        TriagePathway.Client, TriagePathway.Subcontractor, TriagePathway.Supplier, TriagePathway.Internal
    };

    public static string Label(TriagePathway pathway) => pathway.ToString();

    public static TriagePathway? FromBucket(string? bucket)
    {
        if (string.IsNullOrEmpty(bucket)) return null;
        if (bucket.Equals(ClientBucket, StringComparison.OrdinalIgnoreCase)) return TriagePathway.Client;
        if (bucket.Equals(SubcontractorBucket, StringComparison.OrdinalIgnoreCase)) return TriagePathway.Subcontractor;
        if (bucket.Equals(SupplierBucket, StringComparison.OrdinalIgnoreCase)) return TriagePathway.Supplier;
        if (bucket.Equals(InternalBucket, StringComparison.OrdinalIgnoreCase)) return TriagePathway.Internal;
        return null;
    }

    /// <summary>The pathway's Tone — Client positive, Subcontractor warning, Supplier info,
    /// Internal muted — so the pathway pill reads the same in the queue, the reading pane and
    /// every register (Pill + StatusTones.PathwayTone share this reading).</summary>
    public static Jewel.JPMS.Components.Tone Tone(TriagePathway pathway) => pathway switch
    {
        TriagePathway.Client        => Jewel.JPMS.Components.Tone.Positive,
        TriagePathway.Subcontractor => Jewel.JPMS.Components.Tone.Warning,
        TriagePathway.Supplier      => Jewel.JPMS.Components.Tone.Info,
        _                           => Jewel.JPMS.Components.Tone.Muted
    };
}

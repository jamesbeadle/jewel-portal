namespace Jewel.JPMS.Api.Features.Bluebeam.Queue;

/// <summary>Storage queue names used by the Bluebeam extraction pipeline.</summary>
public static class BluebeamQueues
{
    /// <summary>Drawing revisions waiting for markup + text extraction (worker-consumed — the
    /// session dance takes minutes, far past the SWA gateway's patience).</summary>
    public const string DrawingExtractions = "drawing-extractions";
}

/// <summary>One extraction to run. Force re-runs a revision that already succeeded (the UI's
/// re-extract); without it a duplicate delivery of the same message is a harmless no-op.
/// RowsOnly (2026-09-16) skips the PDF entirely and re-transcribes the rows from the structure
/// blob already in storage — the rebuild for revisions extracted before the rows existed.
/// Both flags default off so a message queued by an older api still deserialises.</summary>
public sealed record DrawingExtractionMessage(
    string DrawingRevisionId,
    string RequestedBy,
    bool Force = false,
    bool RowsOnly = false);

namespace Jewel.JPMS.Models;

/// <summary>
/// Where an inbound tender enquiry stands — the client-side journey from an architect's invitation
/// to a priced tender. Values are persisted as ints: append only, never reorder.
/// </summary>
public enum TenderEnquiryStatus
{
    Received = 0,         // logged and in hand — the response is being prepared
    Accepted = 1,         // RETIRED 2026-08-25 (James: logging IS accepting) — never set; kept so persisted ints hold
    Declined = 2,         // Jewel passed on the enquiry
    PqqSubmitted = 3,     // the pre-qualification questionnaire has gone back to the architect
    Shortlisted = 4,      // invited onto the tender list — pricing the job
    NotShortlisted = 5,   // the PQQ did not make the tender list
    TenderSubmitted = 6,  // the priced tender has gone in
    Won = 7,              // the job is Jewel's — the project moves on from Lead
    Lost = 8              // the tender went elsewhere
}

public static class TenderEnquiryStatusExtensions
{
    public static string DisplayName(this TenderEnquiryStatus status) => status switch
    {
        TenderEnquiryStatus.Received        => "Received",
        TenderEnquiryStatus.Accepted        => "Accepted",
        TenderEnquiryStatus.Declined        => "Declined",
        TenderEnquiryStatus.PqqSubmitted    => "PQQ submitted",
        TenderEnquiryStatus.Shortlisted     => "Shortlisted",
        TenderEnquiryStatus.NotShortlisted  => "Not shortlisted",
        TenderEnquiryStatus.TenderSubmitted => "Tender submitted",
        TenderEnquiryStatus.Won             => "Won",
        TenderEnquiryStatus.Lost            => "Lost",
        _ => status.ToString()
    };

    public static string AccentDotClass(this TenderEnquiryStatus status) => status switch
    {
        TenderEnquiryStatus.Received        => "bg-slate-400",
        TenderEnquiryStatus.Accepted        => "bg-sky-500",
        TenderEnquiryStatus.Declined        => "bg-slate-500",
        TenderEnquiryStatus.PqqSubmitted    => "bg-amber-500",
        TenderEnquiryStatus.Shortlisted     => "bg-violet-600",
        TenderEnquiryStatus.NotShortlisted  => "bg-rose-400",
        TenderEnquiryStatus.TenderSubmitted => "bg-emerald-500",
        TenderEnquiryStatus.Won             => "bg-slate-900",
        TenderEnquiryStatus.Lost            => "bg-rose-500",
        _ => "bg-slate-400"
    };

    /// <summary>True while the enquiry is still live business — anything short of an ending.</summary>
    public static bool IsOpen(this TenderEnquiryStatus status) =>
        status is not (TenderEnquiryStatus.Declined
            or TenderEnquiryStatus.NotShortlisted
            or TenderEnquiryStatus.Won
            or TenderEnquiryStatus.Lost);

    /// <summary>A value that exists only for rows written before it was retired; never offered,
    /// never accepted as a move.</summary>
    public static bool IsRetired(this TenderEnquiryStatus status) => status == TenderEnquiryStatus.Accepted;

    /// <summary>Every status a user may set, in journey order — any move is allowed in either
    /// direction (a wrong press is undone by choosing the right one); an ending just needs a
    /// reason.</summary>
    public static IReadOnlyList<TenderEnquiryStatus> Choices { get; } = new[]
    {
        TenderEnquiryStatus.Received,
        TenderEnquiryStatus.PqqSubmitted,
        TenderEnquiryStatus.Shortlisted,
        TenderEnquiryStatus.NotShortlisted,
        TenderEnquiryStatus.TenderSubmitted,
        TenderEnquiryStatus.Won,
        TenderEnquiryStatus.Lost,
        TenderEnquiryStatus.Declined
    };
}

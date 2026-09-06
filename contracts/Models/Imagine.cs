namespace Jewel.JPMS.Models;

/// <summary>
/// "Imagine" (2026-09-06): what happens after a lead has been identified. Each lead carries one
/// private link — <c>/imagine/{token}</c>, printed as a QR code on the letter or brochure — and
/// only that link opens the page: no general address exists, so a page always knows which lead
/// it belongs to. There the prospect uploads photos of their house or plot, says what they are
/// dreaming of, and leaves an email; the worker asks Claude to look at the photos and write
/// three concepts, has Azure image generation render each one over their own photo, and emails
/// them the link back. They pick one, say what they'd change, and a revision round follows. Each
/// round is logged on the lead's timeline, and the first submission moves the lead to Engaged —
/// a person who has uploaded their own house is talking to us.
/// </summary>
public enum ImagineRoundKind
{
    /// <summary>The first round: the prospect's photos and brief → three concepts.</summary>
    Concepts = 0,
    /// <summary>A later round: one chosen concept plus what they would change → a revision.</summary>
    Revision = 1
}

/// <summary>Where a round is. Values persist as ints — append only.</summary>
public enum ImagineRoundStatus
{
    Queued = 0,
    Running = 1,
    Complete = 2,
    Failed = 3
}

public static class ImagineRoundStatusExtensions
{
    public static bool IsInProgress(this ImagineRoundStatus status) =>
        status is ImagineRoundStatus.Queued or ImagineRoundStatus.Running;

    public static string DisplayName(this ImagineRoundStatus status) => status switch
    {
        ImagineRoundStatus.Queued   => "Queued",
        ImagineRoundStatus.Running  => "Rendering",
        ImagineRoundStatus.Complete => "Ready",
        ImagineRoundStatus.Failed   => "Failed",
        _ => status.ToString()
    };
}

/// <summary>What an image is: a photo the prospect uploaded, or a concept the AI rendered.</summary>
public enum ImagineImageKind
{
    Photo = 0,
    Concept = 1
}

/// <summary>One image on a round. The bytes are served by the API (public: keyed by the lead's
/// token; staff: keyed by the lead id) — the record carries only the id and what it is.</summary>
public sealed record ImagineImageView(
    string ImageId,
    ImagineImageKind Kind,
    // Concepts: a short name and the idea in a couple of sentences, written by Claude.
    string Title,
    string Description,
    int Order,
    // The prospect's reaction — the pick and what they said about it.
    bool Liked,
    string Comment);

/// <summary>One round — a submission and what came back — as both the public page and the lead
/// page read it.</summary>
public sealed record ImagineRoundView(
    string RoundId,
    int Number,
    ImagineRoundKind Kind,
    // Concepts round: what the prospect wrote about the house and the dream. Revision round:
    // what they'd change about the chosen concept.
    string Brief,
    // Revision rounds: the concept image they chose to build on.
    string? BasedOnImageId,
    ImagineRoundStatus Status,
    string? Error,
    DateTimeOffset RequestedAt,
    DateTimeOffset? CompletedAt,
    // Claude's reading of the photos — what it saw, in a paragraph — shown above the concepts.
    string Observations,
    IReadOnlyList<ImagineImageView> Photos,
    IReadOnlyList<ImagineImageView> Concepts);

/// <summary>The public page's read: enough for the prospect to see their rounds and act, and
/// nothing that identifies the lead beyond what they typed themselves. Limits travel so the page
/// can say "one more revision" honestly.</summary>
public sealed record ImagineView(
    // The prospect's own name as we hold it — a letter addressed to them says it already.
    string Greeting,
    string ProspectEmail,
    IReadOnlyList<ImagineRoundView> Rounds,
    int RoundsRemaining,
    int MaxPhotosPerRound,
    // The proposal, once one has been sent to this lead (null before then).
    ProposalView? Proposal);

/// <summary>What the prospect submits on the first round: photos (already downscaled in the
/// browser, as data URLs or bare base64 JPEG/PNG), the brief, their name and email.</summary>
public sealed record ImagineSubmission(
    string Name,
    string Email,
    string Brief,
    IReadOnlyList<ImaginePhotoUpload> Photos,
    bool Consent);

/// <summary>One uploaded photo: base64 bytes and the type the browser reported.</summary>
public sealed record ImaginePhotoUpload(string FileName, string ContentType, string Base64);

/// <summary>A revision request: the concept to build on and what to change.</summary>
public sealed record ImagineRevisionRequest(string ImageId, string Feedback);

/// <summary>The prospect's reaction to one concept: a pick and/or a comment.</summary>
public sealed record ImagineReaction(string ImageId, bool Liked, string Comment);

/// <summary>What the lead page knows about the lead's imagine link and rounds.</summary>
public sealed record LeadImagine(
    string? Token,
    DateTimeOffset? IssuedAt,
    IReadOnlyList<ImagineRoundView> Rounds);

public static class ImagineLimits
{
    public const int MaxPhotosPerRound = 6;
    /// <summary>Per photo, after the browser has downscaled it — a 1600px JPEG is well under this.</summary>
    public const int MaxPhotoBytes = 3_000_000;
    /// <summary>Concepts round plus revisions, per lead. A prospect who wants more talks to us.</summary>
    public const int MaxRoundsPerLead = 4;
    public const int MaxBriefLength = 4000;
    public const int ConceptsPerRound = 3;
}

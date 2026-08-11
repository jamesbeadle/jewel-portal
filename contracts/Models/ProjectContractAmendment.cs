namespace Jewel.JPMS.Models;

/// <summary>
/// A formal amendment to the project's contract — a deed of variation, a side letter, a signed
/// supplemental agreement. Each is its own record with its own document, in the order they were
/// made: the executed contract on <see cref="ProjectContract"/> stays exactly as signed, and the
/// amendments beside it are the history of how the bargain moved afterwards.
///
/// <para>Deliberately NOT a version chain on the contract document (see
/// <c>AttachProjectContractDocumentHandler</c> — replacing that document means the wrong file was
/// uploaded). An amendment is a real event with a date and a document of its own.</para>
///
/// <para>The current contract terms live on <see cref="ProjectContract"/> and are edited there when
/// an amendment changes them — this record is the evidence, not a second source of truth.</para>
/// </summary>
public sealed record ProjectContractAmendment(
    string ProjectContractAmendmentId,
    string ProjectId,

    // ---- What it is ----
    string Title,                        // "Deed of Variation No. 1", "Side letter — revised LADs"
    DateTimeOffset? AmendmentDate,       // The date the amendment was made, not the upload date.
    string? Notes,                       // What it changed, in a sentence or two.

    // ---- The document ----
    string DocumentFileName,
    string DocumentContentType,
    long DocumentFileSizeBytes,
    DateTimeOffset DocumentUploadedAt,
    string DocumentUploadedByEmail,

    string? UpdatedByEmail,
    DateTimeOffset UpdatedAt);

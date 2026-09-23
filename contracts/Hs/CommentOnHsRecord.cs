using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Contracts.Progress;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Hs;

/// <summary>
/// A comment on a record's thread — the site manager saying he is on it or what is stopping him,
/// the officer answering. The author is stamped by the door it came through (the endpoint from
/// the sign-in, the connector from the caller); a comment on an Open corrective action moves it
/// to In progress by itself. Photographs come through the multipart door on the same route.
/// </summary>
public sealed record CommentOnHsRecord(
    string HsRecordId,
    string Text,
    string AuthorEmail = "",
    string AuthorName = "") : ICommand<HsRecordComment>;

/// <summary>What the multipart door answers: the comment as saved, and one outcome per photograph posted.</summary>
public sealed record HsRecordCommentWithPhotos(HsRecordComment Comment, IReadOnlyList<ProgressPhotoIntakeOutcome> Photos);

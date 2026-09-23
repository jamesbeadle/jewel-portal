using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Api.Features.Hs.Notifications;
using Jewel.JPMS.Contracts.Hs;

namespace Jewel.JPMS.Api.Features.Hs.Thread.Commands;

/// <summary>
/// A comment on a record's thread, from words alone — the connector's door and the page's when no
/// photograph is sent. Writes the comment, the event the digest reads, and moves an Open corrective
/// action to In progress: someone writing on it is working on it. The multipart door stores its
/// photographs against the comment this returns, in the same save.
/// </summary>
public sealed class CommentOnHsRecordHandler : ICommandHandler<CommentOnHsRecord, HsRecordComment>
{
    private readonly JpmsContext context;
    public CommentOnHsRecordHandler(JpmsContext context) { this.context = context; }

    public async Task<HsRecordComment> HandleAsync(CommentOnHsRecord command, CancellationToken cancellationToken)
    {
        var comment = await StageAsync(command, DateTimeOffset.UtcNow, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
        return comment.ToModel(Array.Empty<HsRecordPhotoEntity>());
    }

    /// <summary>Everything the comment writes, added to the context and not yet saved, so a door that also stores photographs commits once.</summary>
    public async Task<HsRecordCommentEntity> StageAsync(CommentOnHsRecord command, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var record = await context.HsRecords.FirstOrDefaultAsync(row => row.HsRecordId == command.HsRecordId, cancellationToken)
            ?? throw new InvalidOperationException("That record no longer exists.");
        var comment = new HsRecordCommentEntity
        {
            HsRecordCommentId = HsIdentifierFactory.NextHsRecordCommentId(),
            HsRecordId = record.HsRecordId,
            ProjectId = record.ProjectId,
            AuthorEmail = command.AuthorEmail.Trim(),
            AuthorName = command.AuthorName.Trim(),
            Text = command.Text.Trim(),
            PostedAt = now
        };
        context.HsRecordComments.Add(comment);
        HsRecordEvents.Record(context, record, HsRecordEventKind.Commented, comment.Text, command.AuthorEmail, command.AuthorName, now);
        MoveToInProgress(record, command, now);
        return comment;
    }

    private void MoveToInProgress(HsRecordEntity record, CommentOnHsRecord command, DateTimeOffset now)
    {
        var isAnOpenAction = record.Kind == (int)HsRecordKind.CorrectiveAction && record.Status == (int)HsStatus.Open;
        if (!isAnOpenAction) return;
        record.Status = (int)HsStatus.InProgress;
        var detail = HsRecordEvents.StatusChangeDetail(HsStatus.Open, HsStatus.InProgress);
        HsRecordEvents.Record(context, record, HsRecordEventKind.StatusChanged, detail, command.AuthorEmail, command.AuthorName, now);
    }
}

public sealed class CommentOnHsRecordAuthorisation
{
    public bool Allows(SignedInUser user, CommentOnHsRecord command) => HsActionRoles.Contributors.IncludesAny(user.Roles);
}

public sealed class CommentOnHsRecordValidation
{
    public const int LongestComment = 4000;

    public ValidationOutcome Check(CommentOnHsRecord command)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(command.HsRecordId)) errors.Add("HsRecordId is required.");
        if (string.IsNullOrWhiteSpace(command.Text)) errors.Add("Say something — a comment needs words.");
        if (command.Text.Length > LongestComment) errors.Add($"A comment is at most {LongestComment} characters.");
        if (errors.Count == 0) return ValidationOutcome.Passed;
        return new ValidationOutcome(errors);
    }
}

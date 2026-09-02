using Jewel.JPMS.Api.Features.Audit;
using Jewel.JPMS.Api.Features.MailboxIntake.Graph;
using Jewel.JPMS.Api.Features.Requests;
using Jewel.JPMS.Contracts.Requests;

namespace Jewel.JPMS.Api.Features.MailboxIntake.Compose;

public sealed partial class SendMailboxEmailHandler
{
    /// <summary>The optional record filing — tag-first and verified, the recoverable half: a
    /// General request raised from the replied-to email, or the thread linked to an existing
    /// record. Neither, and nothing happens here.</summary>
    private async Task FileToRecordAsync(Compose compose, CancellationToken cancellationToken)
    {
        var command = compose.Command;
        if (command.AlsoRaiseRequest)
            await RaiseRequestAsync(compose, cancellationToken);
        else if (command.LinkRecordType is { } linkType && !string.IsNullOrWhiteSpace(command.LinkRecordId))
            await LinkRecordAsync(compose, linkType, cancellationToken);
    }

    // The old "Reply in thread" composite, now opt-in: create the General request exactly as
    // "Create new → Request" would (email + thread tagged first, anchor verified), carrying
    // the written reply as its description.
    private async Task RaiseRequestAsync(Compose compose, CancellationToken cancellationToken)
    {
        var command = compose.Command;
        var snapshot = compose.Snapshot!;
        compose.RaisedRequest = await createRequest.HandleAsync(
            new CreateRequestFromMessage(
                command.ReplyToMessageId!,
                command.ProjectId!,
                RequestType.General,
                Reference: "",
                Title: string.IsNullOrWhiteSpace(snapshot.Subject) ? "(no subject)" : snapshot.Subject.Trim(),
                Description: $"Replied to email in thread with:\n\n{PlainTextOf(command)}",
                InternetMessageId: command.ReplyToInternetMessageId ?? snapshot.InternetMessageId,
                RaisedByEmail: command.SenderEmail),
            cancellationToken);

        compose.RecordTag = TriageCategories.ForRecord(
            RequestTags.Stem(
                await RequestTags.ProjectRefAsync(context, command.ProjectId!, cancellationToken),
                command.ProjectId!,
                compose.RaisedRequest.Reference.Trim()));
    }

    private async Task LinkRecordAsync(Compose compose, RecordType linkType, CancellationToken cancellationToken)
    {
        var command = compose.Command;
        var linkedRecord = await providers.For(linkType).FindAsync(command.LinkRecordId!, cancellationToken)
            ?? throw new InvalidOperationException($"{linkType} record '{command.LinkRecordId}' not found.");
        compose.LinkedRecord = linkedRecord;
        compose.RecordTag = TriageCategories.ForRecord(linkedRecord.TagReference);
        var recordBucket = TriageCategories.BucketFor(linkType) ?? compose.ChosenBucket;

        if (compose.ExistingBucket is not null && recordBucket is not null
            && TriageCategories.CrossesClientWall(compose.ExistingBucket, recordBucket))
            throw new InvalidOperationException(
                $"This thread is filed under {AuditTrail.PathwayLabel(compose.ExistingBucket)}; {linkedRecord.Reference} would file it under {AuditTrail.PathwayLabel(recordBucket)}. "
                + "Client correspondence is never mixed with subcontractor or internal correspondence.");
        compose.EffectiveBucket = compose.ExistingBucket ?? recordBucket ?? compose.EffectiveBucket;

        if (!compose.IsReply) return;
        // File the inbound thread to the record now (anchor verified) — same tagging as a
        // triage link, and recoverable: if the send later fails, the thread is filed but
        // unanswered, which the outcome reports honestly.
        var snapshot = compose.Snapshot!;
        var tagged = await threadTagger.TagThreadAsync(
            command.ReplyToMessageId!, snapshot.InternetMessageId, snapshot.ConversationId,
            compose.RecordTag, cancellationToken, anchorReceivedAt: snapshot.ReceivedAt);
        if (!tagged)
            throw new InvalidOperationException("The email couldn't be tagged to the record. Nothing was sent — please try again.");
    }

    private async Task RollBackRaisedRequestAsync(Request? raised, string? tag, CancellationToken ct)
    {
        if (raised is null) return;
        // Best-effort: pull the tags back off so the email returns to the queue, then delete the
        // request — half-triaged (request created, nothing sent) is worse than not triaged at all.
        if (tag is not null)
            try { await graph.ClearRequestTagsAsync(tag, ct); } catch { /* best-effort */ }
        var entity = await context.Requests.FirstOrDefaultAsync(r => r.RequestId == raised.RequestId, ct);
        if (entity is not null)
        {
            context.Requests.Remove(entity);
            await context.SaveChangesAsync(ct);
        }
    }
}

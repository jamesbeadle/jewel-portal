using Jewel.JPMS.Api.Features.MailboxIntake.Graph;
using Jewel.JPMS.Contracts.Requests;

namespace Jewel.JPMS.Api.Features.Requests.Commands;

public sealed partial class ReplyInThreadFromMessageHandler
{
    /// <summary>The General request exactly as "Create new → Request" would raise it: auto-numbered
    /// REQ-#### (blank reference), email tagged to it first and verified before the request
    /// persists. This is the background half of the action — the paper trail for the reply, whose
    /// description records HOW the email was triaged, so the request page reads as "this is what we
    /// answered".</summary>
    private Task<Request> RaisedRequestAsync(
        ReplyInThreadFromMessage command, MailboxSnapshot snapshot, string reply, CancellationToken cancellationToken) =>
        createRequest.HandleAsync(
            new CreateRequestFromMessage(
                command.MessageId,
                command.ProjectId,
                RequestType.General,
                Reference: "",
                Title: string.IsNullOrWhiteSpace(snapshot.Subject) ? "(no subject)" : snapshot.Subject.Trim(),
                Description: $"Replied to email in thread with:\n\n{reply}",
                InternetMessageId: command.InternetMessageId ?? snapshot.InternetMessageId,
                RaisedByEmail: command.RaisedByEmail),
            cancellationToken);

    /// <summary>The same project-qualified workflow tag the create path stamped on the email,
    /// re-derived here for the draft's categories (a General request's minted reference is never
    /// blank).</summary>
    private async Task<string> WorkflowTagAsync(
        ReplyInThreadFromMessage command, Request request, CancellationToken cancellationToken) =>
        TriageCategories.ForRecord(
            RequestTags.Stem(
                await RequestTags.ProjectRefAsync(context, command.ProjectId, cancellationToken),
                command.ProjectId,
                request.Reference.Trim()));

    /// <summary>The reply is staged, so the ball has moved: the request the triage created is a
    /// General container raised at Needs action (an email arriving IS ours to act on), and drafting
    /// the answer is that action being taken. It moves to Open — with the correspondent, awaiting
    /// their response — exactly as the two document-draft paths do. The team sets it back to Needs
    /// action by hand if the draft is never sent. Only Needs action moves, so a re-triage can never
    /// rewind a request that has already moved on.</summary>
    private async Task<Request> MovedToOpenAsync(Request request, CancellationToken cancellationToken)
    {
        var raised = await context.Requests.FirstOrDefaultAsync(row => row.RequestId == request.RequestId, cancellationToken);
        if (raised is null || (RequestStatus)raised.Status != RequestStatus.NeedsAction) return request;
        raised.Status = (int)RequestStatus.Open;
        await context.SaveChangesAsync(cancellationToken);
        return request with { Status = RequestStatus.Open };
    }
}

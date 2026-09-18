using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Api.Features.MailboxIntake;
using Jewel.JPMS.Api.Features.MailboxIntake.Compose;
using Jewel.JPMS.Api.Features.MailboxIntake.Sharing;
using Jewel.JPMS.Api.Features.Requests.Documents;
using Jewel.JPMS.Contracts.Requests;

namespace Jewel.JPMS.Api.Features.Requests.Commands;

/// <summary>
/// Writes the request document's email: who it goes to (.Recipients), what leaves with it (.Draft
/// and .Attachments) and what it says (.CoverNote). The door that sends it
/// (<see cref="SendRequestEmailHandler"/>) and the read that previews it both come through here,
/// so what a person reads before pressing Send is the message that will be staged.
/// </summary>
public sealed partial class RequestEmailComposer : IComposesRecordEmail
{
    private readonly JpmsContext context;
    private readonly MailboxIntakeOptions mailboxOptions;
    private readonly Attachments.IRequestAttachmentStore attachmentStore;
    private readonly IEmailFileShareStore shareStore;

    public RequestEmailComposer(
        JpmsContext context,
        MailboxIntakeOptions mailboxOptions,
        Attachments.IRequestAttachmentStore attachmentStore,
        IEmailFileShareStore shareStore)
    {
        this.context = context;
        this.mailboxOptions = mailboxOptions;
        this.attachmentStore = attachmentStore;
        this.shareStore = shareStore;
    }

    public RecordType Record => RecordType.Request;

    public RoleSet RolesThatMaySend => SendRequestEmailAuthorisation.RolesThatMayDraft;

    /// <summary>A request document's subject and cover note are always the portal's own words — the
    /// document is the message — so a draft's Subject and BodyHtml are not read here.</summary>
    public async Task<ComposedRecordEmail> ComposeAsync(RecordEmailDraft draft, CancellationToken cancellationToken)
    {
        var request = await EmailableRequestAsync(draft.RecordId, cancellationToken);
        var recipients = await RecipientsForAsync(request, draft.RecipientOverride, cancellationToken);

        var model = await RequestDocumentBuilder.BuildAsync(context, draft.RecordId, cancellationToken, recipients)
            ?? throw new InvalidOperationException($"Request '{draft.RecordId}' not found.");

        return new ComposedRecordEmail(
            model.DisplayNumber,
            request.ProjectId,
            await StagedDraftAsync(request, model, recipients, cancellationToken),
            CopiedRecipients(recipients));
    }

    /// <summary>EMAIL POLICY: only RFI / NOD / EOT documents are ever drafted for sending.</summary>
    private async Task<RequestEntity> EmailableRequestAsync(string requestId, CancellationToken cancellationToken)
    {
        var request = await context.Requests
            .FirstOrDefaultAsync(row => row.RequestId == requestId, cancellationToken);
        if (request is null) throw new InvalidOperationException($"Request '{requestId}' not found.");

        var kind = (RequestType)request.Kind;
        if (kind.IsEmailable()) return request;

        throw new InvalidOperationException(
            $"A {kind.DisplayName()} request is never emailed — only RFI, NOD and EOT documents " +
            "are drafted for sending. Promote the request first if it should go out as an RFI.");
    }
}

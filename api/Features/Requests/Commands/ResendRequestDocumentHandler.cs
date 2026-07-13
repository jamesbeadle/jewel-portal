using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Features.MailboxIntake.Actions;
using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Contracts.Requests;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Requests.Commands;

public sealed class ResendRequestDocumentHandler : ICommandHandler<ResendRequestDocument, Acknowledgement>
{
    private readonly JpmsContext context;
    private readonly IMailboxActionScheduler mailbox;

    public ResendRequestDocumentHandler(JpmsContext context, IMailboxActionScheduler mailbox)
    {
        this.context = context;
        this.mailbox = mailbox;
    }

    public async Task<Acknowledgement> HandleAsync(ResendRequestDocument command, CancellationToken cancellationToken)
    {
        var request = await context.Requests
            .Where(r => r.RequestId == command.RequestId)
            .Select(r => new { r.Kind })
            .FirstOrDefaultAsync(cancellationToken);
        if (request is null) throw new InvalidOperationException($"Request '{command.RequestId}' not found.");

        var kind = (RequestType)request.Kind;
        if (!kind.IsEmailable())
            throw new InvalidOperationException(
                $"A {kind.DisplayName()} request is never emailed — only RFI, NOD and EOT documents " +
                "are drafted for sending. Promote the request first if it should go out as an RFI.");

        // The PDF is regenerated from SQL by the worker, so the resend carries only the request id and
        // the optional ad-hoc recipient. A normalised empty override means "use the project's contacts".
        var recipientOverride = string.IsNullOrWhiteSpace(command.RecipientOverride) ? null : command.RecipientOverride.Trim();
        await mailbox.ScheduleRequestDocumentSendAsync(command.RequestId, recipientOverride, cancellationToken);

        return new Acknowledgement(command.RequestId);
    }
}

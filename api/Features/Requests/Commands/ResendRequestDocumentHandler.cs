using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Features.MailboxIntake.Actions;
using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Contracts.Requests;
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
        var exists = await context.Requests.AnyAsync(r => r.RequestId == command.RequestId, cancellationToken);
        if (!exists) throw new InvalidOperationException($"Request '{command.RequestId}' not found.");

        // The PDF is regenerated from SQL by the worker, so the resend carries only the request id and
        // the optional ad-hoc recipient. A normalised empty override means "use the project's contacts".
        var recipientOverride = string.IsNullOrWhiteSpace(command.RecipientOverride) ? null : command.RecipientOverride.Trim();
        await mailbox.ScheduleRequestDocumentSendAsync(command.RequestId, recipientOverride, cancellationToken);

        return new Acknowledgement(command.RequestId);
    }
}

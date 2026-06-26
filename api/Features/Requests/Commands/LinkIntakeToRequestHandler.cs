using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Api.Features.MailboxIntake.Actions;
using Jewel.JPMS.Contracts.Requests;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Requests.Commands;

public sealed class LinkIntakeToRequestHandler : ICommandHandler<LinkIntakeToRequest, IntakeEmail>
{
    private readonly JpmsContext context;
    private readonly IMailboxActionScheduler mailbox;
    public LinkIntakeToRequestHandler(JpmsContext context, IMailboxActionScheduler mailbox) { this.context = context; this.mailbox = mailbox; }

    public async Task<IntakeEmail> HandleAsync(LinkIntakeToRequest command, CancellationToken cancellationToken)
    {
        var intake = await context.IntakeEmails.FirstOrDefaultAsync(e => e.IntakeId == command.IntakeId, cancellationToken)
            ?? throw new InvalidOperationException($"Intake email {command.IntakeId} not found.");

        var requestExists = await context.Requests.AnyAsync(r => r.RequestId == command.RequestId, cancellationToken);
        if (!requestExists) throw new InvalidOperationException($"Request {command.RequestId} not found.");

        context.RequestMessages.Add(IntakeConversation.AsInboundMessage(intake, command.RequestId));

        intake.Status = (int)IntakeStatus.Linked;
        intake.LinkedRequestId = command.RequestId;
        await context.SaveChangesAsync(cancellationToken);

        await mailbox.ScheduleOutcomeMoveAsync(intake.IntakeId, IntakeStatus.Linked, cancellationToken);
        return intake.ToModel();
    }
}

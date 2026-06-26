using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Features.MailboxIntake.Actions;
using Jewel.JPMS.Contracts.Requests;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Requests.Commands;

public sealed class DiscardIntakeEmailHandler : ICommandHandler<DiscardIntakeEmail, IntakeEmail>
{
    private readonly JpmsContext context;
    private readonly IMailboxActionScheduler mailbox;
    public DiscardIntakeEmailHandler(JpmsContext context, IMailboxActionScheduler mailbox) { this.context = context; this.mailbox = mailbox; }

    public async Task<IntakeEmail> HandleAsync(DiscardIntakeEmail command, CancellationToken cancellationToken)
    {
        var entity = await context.IntakeEmails.FirstOrDefaultAsync(e => e.IntakeId == command.IntakeId, cancellationToken)
            ?? throw new InvalidOperationException($"Intake email {command.IntakeId} not found.");

        entity.Status = (int)IntakeStatus.Discarded;
        if (!string.IsNullOrWhiteSpace(command.Notes)) entity.Notes = command.Notes!.Trim();
        await context.SaveChangesAsync(cancellationToken);

        await mailbox.ScheduleOutcomeMoveAsync(entity.IntakeId, IntakeStatus.Discarded, cancellationToken);
        return entity.ToModel();
    }
}

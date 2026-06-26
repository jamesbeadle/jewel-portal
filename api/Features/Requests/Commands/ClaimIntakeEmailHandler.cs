using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Features.MailboxIntake.Actions;
using Jewel.JPMS.Contracts.Requests;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Requests.Commands;

public sealed class ClaimIntakeEmailHandler : ICommandHandler<ClaimIntakeEmail, IntakeEmail>
{
    private readonly JpmsContext context;
    private readonly IMailboxActionScheduler mailbox;
    public ClaimIntakeEmailHandler(JpmsContext context, IMailboxActionScheduler mailbox) { this.context = context; this.mailbox = mailbox; }

    public async Task<IntakeEmail> HandleAsync(ClaimIntakeEmail command, CancellationToken cancellationToken)
    {
        var entity = await context.IntakeEmails.FirstOrDefaultAsync(e => e.IntakeId == command.IntakeId, cancellationToken)
            ?? throw new InvalidOperationException($"Intake email {command.IntakeId} not found.");

        entity.Status = (int)IntakeStatus.Claimed;
        entity.ClaimedByEmail = command.ClaimedByEmail;
        entity.ClaimedAt = DateTimeOffset.UtcNow;
        await context.SaveChangesAsync(cancellationToken);

        // Mirror the outcome in the mailbox (best-effort; never blocks the triage action).
        await mailbox.ScheduleOutcomeMoveAsync(entity.IntakeId, IntakeStatus.Claimed, cancellationToken);
        return entity.ToModel();
    }
}

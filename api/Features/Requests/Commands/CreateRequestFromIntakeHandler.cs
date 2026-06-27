using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Api.Features.MailboxIntake.Actions;
using Jewel.JPMS.Contracts.Requests;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Requests.Commands;

public sealed class CreateRequestFromIntakeHandler : ICommandHandler<CreateRequestFromIntake, Request>
{
    private readonly JpmsContext context;
    private readonly IMailboxActionScheduler mailbox;
    public CreateRequestFromIntakeHandler(JpmsContext context, IMailboxActionScheduler mailbox) { this.context = context; this.mailbox = mailbox; }

    public async Task<Request> HandleAsync(CreateRequestFromIntake command, CancellationToken cancellationToken)
    {
        var intake = await context.IntakeEmails.FirstOrDefaultAsync(e => e.IntakeId == command.IntakeId, cancellationToken)
            ?? throw new InvalidOperationException($"Intake email {command.IntakeId} not found.");

        var nextNumber = (await context.Requests.MaxAsync(r => (int?)r.Number, cancellationToken) ?? 0) + 1;

        var request = new RequestEntity
        {
            RequestId = RequestsIdentifierFactory.Next(),
            Number = nextNumber,
            ProjectId = command.ProjectId,
            Kind = (int)command.Kind,
            Reference = command.Reference,
            Title = Clamp(command.Title, 256),
            Description = Clamp(command.Description, 2048),
            Status = (int)RequestStatus.Open,
            Value = command.Value,
            RaisedByEmail = command.RaisedByEmail,
            RaisedAt = intake.ReceivedAt,
            ImpliesVariation = false,
            RaisedTo = command.RaisedTo,
            DrawingRef = command.DrawingRef,
            ResponseDue = command.ResponseDue
        };
        context.Requests.Add(request);

        // Record the originating email as the opening message of the new request's thread.
        context.RequestMessages.Add(IntakeConversation.AsInboundMessage(intake, request.RequestId));

        intake.Status = (int)IntakeStatus.Linked;
        intake.LinkedRequestId = request.RequestId;

        await context.SaveChangesAsync(cancellationToken);

        await mailbox.ScheduleOutcomeMoveAsync(intake.IntakeId, IntakeStatus.Linked, cancellationToken);

        // Pull any other still-untriaged emails from the same thread onto this request too.
        await ThreadGather.SweepSiblingsAsync(context, mailbox, intake, request.RequestId, cancellationToken);

        return request.ToModel();
    }

    // Email subjects/bodies can exceed the request column limits; clamp so a long
    // email can never throw on save. The full body is preserved on the opening message.
    private static string Clamp(string value, int maxLength) =>
        string.IsNullOrEmpty(value) || value.Length <= maxLength ? value : value[..maxLength];
}

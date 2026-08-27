using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Features.MailboxIntake.Graph;
using Jewel.JPMS.Api.Features.RecordLinks;
using Jewel.JPMS.Contracts.Calendar;
using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Contracts.RecordLinks;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Calendar.Commands;

/// <summary>
/// Raises a calendar event from an email arranging something dated. Order of work: pre-flight the
/// cross-pathway confirm (free to refuse before anything exists); then the event row; then the
/// email tag through the shared link path, so the tag matches the provider. The tag is the only
/// association — the event reads its mail back live by it (the LogTenderEnquiryFromMessage shape).
/// </summary>
public sealed class CreateCalendarEventFromMessageHandler : ICommandHandler<CreateCalendarEventFromMessage, CalendarEvent>
{
    private const string NewRecordLabel = "the new calendar event";

    private readonly JpmsContext context;
    private readonly IMailboxGraphClient graph;
    private readonly CalendarEventRegister register;
    private readonly ICommandHandler<LinkMessageToRecord, Acknowledgement> link;

    public CreateCalendarEventFromMessageHandler(
        JpmsContext context, IMailboxGraphClient graph, CalendarEventRegister register,
        ICommandHandler<LinkMessageToRecord, Acknowledgement> link)
    {
        this.context = context;
        this.graph = graph;
        this.register = register;
        this.link = link;
    }

    public async Task<CalendarEvent> HandleAsync(CreateCalendarEventFromMessage command, CancellationToken cancellationToken)
    {
        var snapshot = await graph.GetSnapshotAsync(command.MessageId, command.InternetMessageId, cancellationToken)
            ?? throw new InvalidOperationException("The email could not be read from the mailbox.");
        CrossPathwayGuard.EnsureConfirmed(
            snapshot.Categories, TriageCategories.BucketFor(RecordType.CalendarEvent), command.AllowCrossPathway, NewRecordLabel);

        var projectExists = await context.Projects.AnyAsync(p => p.ProjectId == command.ProjectId, cancellationToken);
        if (!projectExists) throw new InvalidOperationException($"Project '{command.ProjectId}' not found.");

        var entity = await register.RaiseAsync(command.ProjectId, command.Details, command.CreatedByEmail, cancellationToken);

        await link.HandleAsync(
            new LinkMessageToRecord(
                command.MessageId, RecordType.CalendarEvent, entity.CalendarEventId, command.InternetMessageId,
                AllowCrossPathway: command.AllowCrossPathway, Scope: command.Scope),
            cancellationToken);

        return entity.ToModel();
    }
}

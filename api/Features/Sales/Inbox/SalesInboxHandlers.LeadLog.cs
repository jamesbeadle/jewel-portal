using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Api.Features.Sales.Commands;
using Jewel.JPMS.Contracts.Sales;

namespace Jewel.JPMS.Api.Features.Sales.Inbox;

public sealed class LogSalesEmailToLeadAuthorisation
{
    public bool Allows(SignedInUser user, LogSalesEmailToLead command) => SalesRoles.SalesTeam.IncludesAny(user.Roles);
}

public sealed class LogSalesEmailToLeadValidation
{
    public ValidationOutcome Check(LogSalesEmailToLead command)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(command.MessageId)) errors.Add("MessageId is required.");
        if (string.IsNullOrWhiteSpace(command.LeadId)) errors.Add("LeadId is required.");
        SalesFieldLimits.Check(errors, command.Note ?? "", 2000, "Note");
        return errors.Count == 0 ? ValidationOutcome.Passed : new ValidationOutcome(errors);
    }
}

public sealed class LogSalesEmailToLeadHandler : ICommandHandler<LogSalesEmailToLead, LeadActivity>
{
    private readonly JpmsContext context;
    private readonly ISalesMailbox mailbox;
    public LogSalesEmailToLeadHandler(JpmsContext context, ISalesMailbox mailbox) { this.context = context; this.mailbox = mailbox; }

    public async Task<LeadActivity> HandleAsync(LogSalesEmailToLead command, CancellationToken cancellationToken)
    {
        var lead = await context.Leads.FirstOrDefaultAsync(row => row.LeadId == command.LeadId, cancellationToken)
            ?? throw new InvalidOperationException($"Lead {command.LeadId} not found.");
        var snapshot = await mailbox.GetSnapshotAsync(command.MessageId, cancellationToken)
            ?? throw new InvalidOperationException("That email couldn't be read from the sales mailbox.");
        // A lead found by letter often has no email until they write in.
        if (string.IsNullOrWhiteSpace(lead.ContactEmail) && !string.IsNullOrWhiteSpace(snapshot.FromEmail))
            lead.ContactEmail = snapshot.FromEmail;
        var entity = new LeadActivityEntity
        {
            LeadActivityId = Guid.NewGuid().ToString("N"),
            LeadId = lead.LeadId,
            Kind = (int)LeadActivityKind.Email,
            Summary = $"Email from {snapshot.FromName} <{snapshot.FromEmail}> — \"{snapshot.Subject}\" ({snapshot.ReceivedAt.ToLocalTime():d MMM HH:mm}) in {mailbox.Address}."
                + (string.IsNullOrWhiteSpace(command.Note) ? (string.IsNullOrWhiteSpace(snapshot.BodyPreview) ? "" : $"\n{snapshot.BodyPreview.Trim()}") : $"\n{command.Note.Trim()}"),
            OccurredAt = snapshot.ReceivedAt,
            RecordedByEmail = command.RecordedByEmail
        };
        context.LeadActivities.Add(entity);
        await context.SaveChangesAsync(cancellationToken);
        return entity.ToModel();
    }
}

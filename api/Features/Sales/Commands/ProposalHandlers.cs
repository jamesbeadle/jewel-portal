using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Api.Features.Sales.Imagine;
using Jewel.JPMS.Api.Features.Sales.Proposals;
using Jewel.JPMS.Contracts.Sales;

namespace Jewel.JPMS.Api.Features.Sales.Commands;

// Proposals: save a draft (new version or edit of an unsent one), send it (the prospect is
// emailed the imagine link; the lead moves to Proposal), withdraw it. Acceptance is the
// prospect's, on the public page (ImaginePublicService).

public sealed class SaveSalesProposalAuthorisation
{
    public bool Allows(SignedInUser user, SaveSalesProposal command) => SalesRoles.SalesTeam.IncludesAny(user.Roles);
}

public sealed class SaveSalesProposalValidation
{
    public ValidationOutcome Check(SaveSalesProposal command)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(command.LeadId)) errors.Add("LeadId is required.");
        if (string.IsNullOrWhiteSpace(command.Title)) errors.Add("A title is required.");
        SalesFieldLimits.Check(errors, command.Title, 256, "Title");
        if (command.BasePrice < 0) errors.Add("The base price cannot be negative.");
        if (command.Options is null) errors.Add("Options must be a list (empty is fine).");
        else
        {
            foreach (var option in command.Options)
            {
                if (string.IsNullOrWhiteSpace(option.Name)) errors.Add("Every option needs a name.");
                SalesFieldLimits.Check(errors, option.Name, 256, "Option name");
                SalesFieldLimits.Check(errors, option.Description, 2000, "Option description");
            }
        }
        if (command.Schedule is null) errors.Add("Schedule must be a list (empty is fine).");
        else
        {
            foreach (var phase in command.Schedule)
            {
                if (string.IsNullOrWhiteSpace(phase.Name)) errors.Add("Every phase needs a name.");
                if (phase.StartWeek < 1) errors.Add($"Phase \"{phase.Name}\": the start week must be 1 or later.");
                if (phase.Weeks < 1) errors.Add($"Phase \"{phase.Name}\": a phase runs at least one week.");
            }
        }
        return errors.Count == 0 ? ValidationOutcome.Passed : new ValidationOutcome(errors);
    }
}

public sealed class SaveSalesProposalHandler : ICommandHandler<SaveSalesProposal, SalesProposal>
{
    private readonly JpmsContext context;
    public SaveSalesProposalHandler(JpmsContext context) { this.context = context; }

    public async Task<SalesProposal> HandleAsync(SaveSalesProposal command, CancellationToken cancellationToken)
    {
        var lead = await context.Leads.AsNoTracking().FirstOrDefaultAsync(row => row.LeadId == command.LeadId, cancellationToken)
            ?? throw new InvalidOperationException($"Lead {command.LeadId} not found.");
        if (command.HeroImageId is not null
            && !await context.ImagineImages.AnyAsync(row => row.ImageId == command.HeroImageId && row.LeadId == lead.LeadId, cancellationToken))
            throw new InvalidOperationException("The chosen render isn't one of this lead's.");

        var now = DateTimeOffset.UtcNow;
        // Options keep their ids across edits (the prospect's accepted ids point at them); new
        // ones are minted here.
        var options = command.Options.Select(option => option with
        {
            OptionId = string.IsNullOrWhiteSpace(option.OptionId) ? Guid.NewGuid().ToString("N")[..12] : option.OptionId,
            Name = option.Name.Trim(),
            Description = (option.Description ?? "").Trim()
        }).ToList();
        var schedule = command.Schedule.Select(phase => phase with { Name = phase.Name.Trim() }).ToList();

        SalesProposalEntity entity;
        if (string.IsNullOrWhiteSpace(command.ProposalId))
        {
            var version = (await context.SalesProposals.Where(row => row.LeadId == lead.LeadId).MaxAsync(row => (int?)row.Version, cancellationToken) ?? 0) + 1;
            entity = new SalesProposalEntity
            {
                ProposalId = Guid.NewGuid().ToString("N"),
                LeadId = lead.LeadId,
                Version = version,
                Status = (int)SalesProposalStatus.Draft,
                CreatedByEmail = command.SavedByEmail,
                CreatedAt = now
            };
            context.SalesProposals.Add(entity);
        }
        else
        {
            entity = await context.SalesProposals.FirstOrDefaultAsync(row => row.ProposalId == command.ProposalId && row.LeadId == lead.LeadId, cancellationToken)
                ?? throw new InvalidOperationException("Proposal not found on this lead.");
            if (entity.Status != (int)SalesProposalStatus.Draft)
                throw new InvalidOperationException($"Version {entity.Version} has been {((SalesProposalStatus)entity.Status).DisplayName().ToLowerInvariant()} — save a new version instead.");
        }

        entity.Title = command.Title.Trim();
        entity.Scope = (command.Scope ?? "").Trim();
        entity.BasePrice = command.BasePrice;
        entity.OptionsJson = ProposalMapping.ToJson(options);
        entity.ScheduleJson = ProposalMapping.ToJson(schedule);
        entity.Terms = (command.Terms ?? "").Trim();
        entity.HeroImageId = command.HeroImageId;
        entity.UpdatedAt = now;
        await context.SaveChangesAsync(cancellationToken);
        return entity.ToModel();
    }
}

public sealed class SendSalesProposalAuthorisation
{
    public bool Allows(SignedInUser user, SendSalesProposal command) => SalesRoles.SalesTeam.IncludesAny(user.Roles);
}

public sealed class SendSalesProposalValidation
{
    public ValidationOutcome Check(SendSalesProposal command)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(command.LeadId)) errors.Add("LeadId is required.");
        if (string.IsNullOrWhiteSpace(command.ProposalId)) errors.Add("ProposalId is required.");
        SalesFieldLimits.Check(errors, command.Note ?? "", 2000, "Note");
        return errors.Count == 0 ? ValidationOutcome.Passed : new ValidationOutcome(errors);
    }
}

public sealed class SendSalesProposalHandler : ICommandHandler<SendSalesProposal, SalesProposal>
{
    private readonly JpmsContext context;
    private readonly IImagineNotifier notifier;
    private readonly ImagineNotifierOptions notifierOptions;
    private readonly ILogger<SendSalesProposalHandler> logger;

    public SendSalesProposalHandler(JpmsContext context, IImagineNotifier notifier, ImagineNotifierOptions notifierOptions, ILogger<SendSalesProposalHandler> logger)
    {
        this.context = context; this.notifier = notifier; this.notifierOptions = notifierOptions; this.logger = logger;
    }

    public async Task<SalesProposal> HandleAsync(SendSalesProposal command, CancellationToken cancellationToken)
    {
        var lead = await context.Leads.FirstOrDefaultAsync(row => row.LeadId == command.LeadId, cancellationToken)
            ?? throw new InvalidOperationException($"Lead {command.LeadId} not found.");
        var proposal = await context.SalesProposals.FirstOrDefaultAsync(row => row.ProposalId == command.ProposalId && row.LeadId == lead.LeadId, cancellationToken)
            ?? throw new InvalidOperationException("Proposal not found on this lead.");
        if (proposal.Status != (int)SalesProposalStatus.Draft)
            throw new InvalidOperationException($"Version {proposal.Version} is already {((SalesProposalStatus)proposal.Status).DisplayName().ToLowerInvariant()}.");
        if (string.IsNullOrWhiteSpace(lead.ImagineToken))
            throw new InvalidOperationException("Issue the lead's imagine link first — the proposal is shown on that page.");
        if (string.IsNullOrWhiteSpace(lead.ContactEmail))
            throw new InvalidOperationException("The lead has no contact email to send the proposal to — add one first.");
        if (!notifier.IsConfigured)
            throw new InvalidOperationException("Email isn't configured on the API (CommunicationServicesConnectionString), so the proposal can't be sent.");
        var stage = (LeadStage)lead.Stage;
        if (!stage.IsOpen())
            throw new InvalidOperationException($"The lead is {stage.DisplayName()} — reopen it before sending a proposal.");

        var now = DateTimeOffset.UtcNow;
        var earlier = await context.SalesProposals
            .Where(row => row.LeadId == lead.LeadId && row.ProposalId != proposal.ProposalId && row.Status == (int)SalesProposalStatus.Sent)
            .ToListAsync(cancellationToken);
        foreach (var old in earlier) { old.Status = (int)SalesProposalStatus.Superseded; old.UpdatedAt = now; }

        // Email first: a send that can't reach the prospect shouldn't change the record.
        await notifier.SendProposalAsync(lead.ContactEmail, lead.ContactName, notifierOptions.ImagineLink(lead.ImagineToken), proposal.Title, command.Note, cancellationToken);

        proposal.Status = (int)SalesProposalStatus.Sent;
        proposal.SentAt = now;
        proposal.UpdatedAt = now;
        if (stage < LeadStage.Proposal)
        {
            lead.Stage = (int)LeadStage.Proposal;
            lead.StageChangedAt = now;
        }
        context.LeadActivities.Add(new LeadActivityEntity
        {
            LeadActivityId = Guid.NewGuid().ToString("N"),
            LeadId = lead.LeadId,
            Kind = (int)LeadActivityKind.ProposalSent,
            Summary = $"Proposal v{proposal.Version} \"{proposal.Title}\" sent to {lead.ContactEmail} — base £{proposal.BasePrice:N0}"
                + (earlier.Count > 0 ? $"; v{string.Join(", v", earlier.Select(old => old.Version))} superseded" : "")
                + (string.IsNullOrWhiteSpace(command.Note) ? "." : $". Note: {command.Note.Trim()}"),
            OccurredAt = now,
            RecordedByEmail = command.SentByEmail
        });
        await context.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Proposal {ProposalId} v{Version} sent on lead {LeadId}.", proposal.ProposalId, proposal.Version, lead.LeadId);
        return proposal.ToModel();
    }
}

public sealed class WithdrawSalesProposalAuthorisation
{
    public bool Allows(SignedInUser user, WithdrawSalesProposal command) => SalesRoles.Deciders.IncludesAny(user.Roles);
}

public sealed class WithdrawSalesProposalValidation
{
    public ValidationOutcome Check(WithdrawSalesProposal command)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(command.LeadId)) errors.Add("LeadId is required.");
        if (string.IsNullOrWhiteSpace(command.ProposalId)) errors.Add("ProposalId is required.");
        return errors.Count == 0 ? ValidationOutcome.Passed : new ValidationOutcome(errors);
    }
}

public sealed class WithdrawSalesProposalHandler : ICommandHandler<WithdrawSalesProposal, SalesProposal>
{
    private readonly JpmsContext context;
    public WithdrawSalesProposalHandler(JpmsContext context) { this.context = context; }

    public async Task<SalesProposal> HandleAsync(WithdrawSalesProposal command, CancellationToken cancellationToken)
    {
        var proposal = await context.SalesProposals.FirstOrDefaultAsync(row => row.ProposalId == command.ProposalId && row.LeadId == command.LeadId, cancellationToken)
            ?? throw new InvalidOperationException("Proposal not found on this lead.");
        if (proposal.Status != (int)SalesProposalStatus.Sent)
            throw new InvalidOperationException($"Only a sent proposal can be withdrawn — this one is {((SalesProposalStatus)proposal.Status).DisplayName().ToLowerInvariant()}.");
        var now = DateTimeOffset.UtcNow;
        proposal.Status = (int)SalesProposalStatus.Superseded;
        proposal.UpdatedAt = now;
        context.LeadActivities.Add(new LeadActivityEntity
        {
            LeadActivityId = Guid.NewGuid().ToString("N"),
            LeadId = proposal.LeadId,
            Kind = (int)LeadActivityKind.ProposalSent,
            Summary = $"Proposal v{proposal.Version} \"{proposal.Title}\" withdrawn.",
            OccurredAt = now,
            RecordedByEmail = command.DecidedByEmail
        });
        await context.SaveChangesAsync(cancellationToken);
        return proposal.ToModel();
    }
}

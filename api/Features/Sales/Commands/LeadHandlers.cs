using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Contracts.Clients;
using Jewel.JPMS.Contracts.Projects;
using Jewel.JPMS.Contracts.Sales;

namespace Jewel.JPMS.Api.Features.Sales.Commands;

// The lead register's writes. Every stage move — by hand, or Won — writes a StageChange activity,
// so the lead's timeline is its whole history. Business refusals throw InvalidOperationException,
// which the endpoints read back as 400 (the connector shows the message as-is).

public sealed class CaptureLeadHandler : ICommandHandler<CaptureLead, Lead>
{
    private readonly JpmsContext context;
    public CaptureLeadHandler(JpmsContext context) { this.context = context; }

    public async Task<Lead> HandleAsync(CaptureLead command, CancellationToken cancellationToken)
    {
        string? strategyName = null;
        var strategyId = string.IsNullOrWhiteSpace(command.StrategyId) ? null : command.StrategyId.Trim();
        if (strategyId is not null)
        {
            var strategy = await context.SalesStrategies.AsNoTracking()
                .FirstOrDefaultAsync(row => row.StrategyId == strategyId, cancellationToken);
            if (strategy is null) throw new InvalidOperationException($"Strategy {strategyId} not found.");
            strategyName = strategy.Name;
        }

        // Global sequence (like defect, inventory and site-instruction numbers): max + 1, never a
        // row count, so a deleted row never re-issues a number.
        var nextNumber = (await context.Leads.MaxAsync(row => (int?)row.Number, cancellationToken) ?? 0) + 1;
        var now = DateTimeOffset.UtcNow;
        var entity = new LeadEntity
        {
            LeadId = Guid.NewGuid().ToString("N"),
            Number = nextNumber,
            Reference = $"LD-{nextNumber:0000}",
            ContactName = command.ContactName.Trim(),
            ContactEmail = command.ContactEmail.Trim(),
            ContactPhone = command.ContactPhone.Trim(),
            CompanyName = command.CompanyName.Trim(),
            ProspectKind = (int)command.ProspectKind,
            SiteAddress = command.PropertyAddress.Trim(),
            Postcode = SalesPostcode.Normalise(command.Postcode),
            Summary = command.Summary.Trim(),
            Notes = command.Notes.Trim(),
            // A lead with a strategy came from that strategy, whatever the caller said.
            Source = (int)(strategyId is not null ? LeadSource.Strategy : command.Source),
            StrategyId = strategyId,
            Stage = (int)command.Stage,
            StageChangedAt = now,
            EstimatedValue = command.EstimatedValue,
            OwnerEmail = command.OwnerEmail.Trim(),
            CapturedAt = now
        };
        context.Leads.Add(entity);
        context.LeadActivities.Add(new LeadActivityEntity
        {
            LeadActivityId = Guid.NewGuid().ToString("N"),
            LeadId = entity.LeadId,
            Kind = (int)LeadActivityKind.StageChange,
            Summary = strategyName is null
                ? $"Captured as {((LeadStage)entity.Stage).DisplayName()} — {((LeadSource)entity.Source).DisplayName()}."
                : $"Captured as {((LeadStage)entity.Stage).DisplayName()} — found by strategy \"{strategyName}\".",
            OccurredAt = now,
            RecordedByEmail = entity.OwnerEmail
        });
        await context.SaveChangesAsync(cancellationToken);
        return entity.ToModel(strategyName);
    }
}

public sealed class UpdateLeadHandler : ICommandHandler<UpdateLead, Lead>
{
    private readonly JpmsContext context;
    public UpdateLeadHandler(JpmsContext context) { this.context = context; }

    public async Task<Lead> HandleAsync(UpdateLead command, CancellationToken cancellationToken)
    {
        var entity = await context.Leads.FirstOrDefaultAsync(row => row.LeadId == command.LeadId, cancellationToken)
            ?? throw new InvalidOperationException($"Lead {command.LeadId} not found.");

        string? strategyName = null;
        var strategyId = string.IsNullOrWhiteSpace(command.StrategyId) ? null : command.StrategyId.Trim();
        if (strategyId is not null)
        {
            strategyName = await context.SalesStrategies.AsNoTracking()
                .Where(row => row.StrategyId == strategyId).Select(row => row.Name)
                .FirstOrDefaultAsync(cancellationToken)
                ?? throw new InvalidOperationException($"Strategy {strategyId} not found.");
        }

        entity.ContactName = command.ContactName.Trim();
        entity.ContactEmail = command.ContactEmail.Trim();
        entity.ContactPhone = command.ContactPhone.Trim();
        entity.CompanyName = command.CompanyName.Trim();
        entity.ProspectKind = (int)command.ProspectKind;
        entity.SiteAddress = command.PropertyAddress.Trim();
        entity.Postcode = SalesPostcode.Normalise(command.Postcode);
        entity.Summary = command.Summary.Trim();
        entity.Notes = command.Notes.Trim();
        entity.Source = (int)(strategyId is not null ? LeadSource.Strategy : command.Source);
        entity.StrategyId = strategyId;
        entity.EstimatedValue = command.EstimatedValue;
        entity.OwnerEmail = command.OwnerEmail.Trim();
        await context.SaveChangesAsync(cancellationToken);
        return entity.ToModel(strategyName);
    }
}

public sealed class MoveLeadStageHandler : ICommandHandler<MoveLeadStage, Lead>
{
    private readonly JpmsContext context;
    public MoveLeadStageHandler(JpmsContext context) { this.context = context; }

    public async Task<Lead> HandleAsync(MoveLeadStage command, CancellationToken cancellationToken)
    {
        var entity = await context.Leads.FirstOrDefaultAsync(row => row.LeadId == command.LeadId, cancellationToken)
            ?? throw new InvalidOperationException($"Lead {command.LeadId} not found.");
        if (entity.Stage == (int)LeadStage.Won)
            throw new InvalidOperationException($"{entity.DisplayReference} is Won — it has a client and a project; it cannot be moved back.");

        var from = (LeadStage)entity.Stage;
        if (from == command.Stage && string.IsNullOrWhiteSpace(command.Note))
            return entity.ToModel(await StrategyNameAsync(entity, cancellationToken));

        var now = DateTimeOffset.UtcNow;
        entity.Stage = (int)command.Stage;
        entity.StageChangedAt = now;
        entity.LostReason = command.Stage == LeadStage.Lost ? command.LostReason?.Trim() : null;

        var summary = from == command.Stage
            ? command.Note!.Trim()
            : $"{from.DisplayName()} → {command.Stage.DisplayName()}"
              + (command.Stage == LeadStage.Lost ? $" — {command.LostReason!.Trim()}" : "")
              + (string.IsNullOrWhiteSpace(command.Note) ? "" : $". {command.Note.Trim()}");
        context.LeadActivities.Add(new LeadActivityEntity
        {
            LeadActivityId = Guid.NewGuid().ToString("N"),
            LeadId = entity.LeadId,
            Kind = (int)LeadActivityKind.StageChange,
            Summary = summary,
            OccurredAt = now,
            RecordedByEmail = command.ChangedByEmail
        });
        await context.SaveChangesAsync(cancellationToken);
        return entity.ToModel(await StrategyNameAsync(entity, cancellationToken));
    }

    private Task<string?> StrategyNameAsync(LeadEntity entity, CancellationToken ct) =>
        entity.StrategyId is null
            ? Task.FromResult<string?>(null)
            : context.SalesStrategies.AsNoTracking().Where(row => row.StrategyId == entity.StrategyId)
                .Select(row => row.Name).FirstOrDefaultAsync(ct);
}

public sealed class WinLeadHandler : ICommandHandler<WinLead, LeadWonOutcome>
{
    private readonly JpmsContext context;
    private readonly ICommandHandler<CreateClient, Client> clientCreator;
    private readonly ICommandHandler<CreateProjectShell, Project> projectShellCreator;

    public WinLeadHandler(
        JpmsContext context,
        ICommandHandler<CreateClient, Client> clientCreator,
        ICommandHandler<CreateProjectShell, Project> projectShellCreator)
    {
        this.context = context;
        this.clientCreator = clientCreator;
        this.projectShellCreator = projectShellCreator;
    }

    public async Task<LeadWonOutcome> HandleAsync(WinLead command, CancellationToken cancellationToken)
    {
        var entity = await context.Leads.FirstOrDefaultAsync(row => row.LeadId == command.LeadId, cancellationToken)
            ?? throw new InvalidOperationException($"Lead {command.LeadId} not found.");
        if (entity.Stage == (int)LeadStage.Won)
            throw new InvalidOperationException($"{entity.DisplayReference} is already Won (project {entity.ProjectId}).");

        var reference = command.ProjectReference.Trim();
        if (await context.Projects.AnyAsync(row => row.Reference == reference, cancellationToken))
            throw new InvalidOperationException($"A project with reference {reference} already exists — pick another.");

        // The client account: the company if there is one, else the person — with the contact as
        // its primary correspondent. Each Won lead makes its own client; a repeat client's second
        // project can be re-pointed on the project afterwards.
        var clientName = string.IsNullOrWhiteSpace(entity.CompanyName) ? entity.ContactName : entity.CompanyName;
        var client = await clientCreator.HandleAsync(
            new CreateClient(
                clientName,
                string.IsNullOrWhiteSpace(entity.ContactName) ? null : entity.ContactName,
                string.IsNullOrWhiteSpace(entity.ContactEmail) ? null : entity.ContactEmail),
            cancellationToken);

        var project = await projectShellCreator.HandleAsync(
            new CreateProjectShell(
                Reference: reference,
                Name: command.ProjectName.Trim(),
                ClientName: clientName,
                Organisation: Organisation.JewelBespokeBuild,
                ProjectManagerEmail: string.IsNullOrWhiteSpace(command.ProjectManagerEmail) ? entity.OwnerEmail : command.ProjectManagerEmail.Trim()),
            cancellationToken);

        var now = DateTimeOffset.UtcNow;
        var from = (LeadStage)entity.Stage;
        entity.Stage = (int)LeadStage.Won;
        entity.StageChangedAt = now;
        entity.LostReason = null;
        entity.ClientId = client.ClientId;
        entity.ProjectId = project.ProjectId;
        context.LeadActivities.Add(new LeadActivityEntity
        {
            LeadActivityId = Guid.NewGuid().ToString("N"),
            LeadId = entity.LeadId,
            Kind = (int)LeadActivityKind.StageChange,
            Summary = $"{from.DisplayName()} → Won — client \"{clientName}\" and project {reference} \"{project.Name}\" created."
                      + (string.IsNullOrWhiteSpace(command.Note) ? "" : $" {command.Note.Trim()}"),
            OccurredAt = now,
            RecordedByEmail = command.DecidedByEmail
        });
        await context.SaveChangesAsync(cancellationToken);

        var strategyName = entity.StrategyId is null ? null
            : await context.SalesStrategies.AsNoTracking().Where(row => row.StrategyId == entity.StrategyId)
                .Select(row => row.Name).FirstOrDefaultAsync(cancellationToken);
        return new LeadWonOutcome(entity.ToModel(strategyName), client.ClientId, project.ProjectId);
    }
}

public sealed class LogLeadActivityHandler : ICommandHandler<LogLeadActivity, LeadActivity>
{
    private readonly JpmsContext context;
    public LogLeadActivityHandler(JpmsContext context) { this.context = context; }

    public async Task<LeadActivity> HandleAsync(LogLeadActivity command, CancellationToken cancellationToken)
    {
        if (!await context.Leads.AnyAsync(row => row.LeadId == command.LeadId, cancellationToken))
            throw new InvalidOperationException($"Lead {command.LeadId} not found.");
        var entity = new LeadActivityEntity
        {
            LeadActivityId = Guid.NewGuid().ToString("N"),
            LeadId = command.LeadId,
            Kind = (int)command.Kind,
            Summary = command.Summary.Trim(),
            OccurredAt = command.OccurredAt ?? DateTimeOffset.UtcNow,
            RecordedByEmail = command.RecordedByEmail
        };
        context.LeadActivities.Add(entity);
        await context.SaveChangesAsync(cancellationToken);
        return entity.ToModel();
    }
}

internal static class SalesPostcode
{
    /// <summary>Upper-cased, single-spaced, trimmed — "sw1a1aa" and "SW1A 1AA" file the same.</summary>
    public static string Normalise(string? postcode)
    {
        if (string.IsNullOrWhiteSpace(postcode)) return "";
        var compact = new string(postcode.Where(c => !char.IsWhiteSpace(c)).ToArray()).ToUpperInvariant();
        // UK postcodes: the inward code is always the last three characters.
        return compact.Length > 3 ? compact[..^3] + " " + compact[^3..] : compact;
    }
}

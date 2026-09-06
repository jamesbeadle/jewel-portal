using Jewel.JPMS.Api.Auth;
using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Api.Features.Sales.Imagine;
using Jewel.JPMS.Api.Features.Sales.Research;
using Jewel.JPMS.Contracts.Sales;

namespace Jewel.JPMS.Api.Features.Sales.Commands;

// The staff side of the imagine journey: issue the lead's link (the QR code), and re-queue a
// round whose render failed. Gates and handlers together, like the strategy research command.

public sealed class IssueImagineLinkAuthorisation
{
    public bool Allows(SignedInUser user, IssueImagineLink command) => SalesRoles.SalesTeam.IncludesAny(user.Roles);
}

public sealed class IssueImagineLinkValidation
{
    public ValidationOutcome Check(IssueImagineLink command)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(command.LeadId)) errors.Add("LeadId is required.");
        return errors.Count == 0 ? ValidationOutcome.Passed : new ValidationOutcome(errors);
    }
}

public sealed class IssueImagineLinkHandler : ICommandHandler<IssueImagineLink, Lead>
{
    private readonly JpmsContext context;
    public IssueImagineLinkHandler(JpmsContext context) { this.context = context; }

    public async Task<Lead> HandleAsync(IssueImagineLink command, CancellationToken cancellationToken)
    {
        var entity = await context.Leads.FirstOrDefaultAsync(row => row.LeadId == command.LeadId, cancellationToken)
            ?? throw new InvalidOperationException($"Lead {command.LeadId} not found.");
        var reissue = entity.ImagineToken is not null;
        var now = DateTimeOffset.UtcNow;
        entity.ImagineToken = AuthTokens.NewSecret();
        entity.ImagineTokenIssuedAt = now;
        context.LeadActivities.Add(new LeadActivityEntity
        {
            LeadActivityId = Guid.NewGuid().ToString("N"),
            LeadId = entity.LeadId,
            Kind = (int)LeadActivityKind.Imagine,
            Summary = reissue
                ? "Imagine link re-issued — the QR code printed before this no longer works."
                : "Imagine link issued — print the QR code on the letter or brochure.",
            OccurredAt = now,
            RecordedByEmail = command.IssuedByEmail
        });
        await context.SaveChangesAsync(cancellationToken);
        var strategyName = entity.StrategyId is null ? null
            : await context.SalesStrategies.AsNoTracking().Where(s => s.StrategyId == entity.StrategyId).Select(s => s.Name).FirstOrDefaultAsync(cancellationToken);
        return entity.ToModel(strategyName);
    }
}

public sealed class RetryImagineRoundAuthorisation
{
    public bool Allows(SignedInUser user, RetryImagineRound command) => SalesRoles.SalesTeam.IncludesAny(user.Roles);
}

public sealed class RetryImagineRoundValidation
{
    public ValidationOutcome Check(RetryImagineRound command)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(command.LeadId)) errors.Add("LeadId is required.");
        if (string.IsNullOrWhiteSpace(command.RoundId)) errors.Add("RoundId is required.");
        return errors.Count == 0 ? ValidationOutcome.Passed : new ValidationOutcome(errors);
    }
}

public sealed class RetryImagineRoundHandler : ICommandHandler<RetryImagineRound, ImagineRoundView>
{
    private readonly JpmsContext context;
    private readonly IImagineRenderQueue queue;
    public RetryImagineRoundHandler(JpmsContext context, IImagineRenderQueue queue) { this.context = context; this.queue = queue; }

    public async Task<ImagineRoundView> HandleAsync(RetryImagineRound command, CancellationToken cancellationToken)
    {
        var round = await context.ImagineRounds.FirstOrDefaultAsync(row => row.RoundId == command.RoundId && row.LeadId == command.LeadId, cancellationToken)
            ?? throw new InvalidOperationException("Round not found on this lead.");
        if (round.Status == (int)ImagineRoundStatus.Complete) throw new InvalidOperationException("That round is complete — nothing to retry.");
        if (round.Status == (int)ImagineRoundStatus.Running && round.StartedAt is { } started && started > DateTimeOffset.UtcNow.AddMinutes(-15))
            throw new InvalidOperationException("That round is rendering now — give it a few minutes.");
        if (!queue.IsConfigured) throw new InvalidOperationException("The render queue isn't configured on the API.");
        round.Status = (int)ImagineRoundStatus.Queued;
        round.Error = null;
        round.StartedAt = null;
        round.CompletedAt = null;
        context.LeadActivities.Add(new LeadActivityEntity
        {
            LeadActivityId = Guid.NewGuid().ToString("N"),
            LeadId = round.LeadId,
            Kind = (int)LeadActivityKind.Imagine,
            Summary = $"Imagine round {round.Number} re-queued.",
            OccurredAt = DateTimeOffset.UtcNow,
            RecordedByEmail = command.RequestedByEmail
        });
        await context.SaveChangesAsync(cancellationToken);
        await queue.EnqueueAsync(new ImagineRenderMessage(round.RoundId), cancellationToken);
        var images = await context.ImagineImages.AsNoTracking().Where(row => row.RoundId == round.RoundId).ToListAsync(cancellationToken);
        return round.ToView(images);
    }
}

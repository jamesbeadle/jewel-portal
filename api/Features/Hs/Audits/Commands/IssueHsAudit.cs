using Jewel.JPMS.Contracts.Hs;

namespace Jewel.JPMS.Api.Features.Hs.Audits.Commands;

/// <summary>The officer's declaration. Draft → Issued, and every unlinked finding becomes a
/// corrective action on the project's H&S register, linked back to its item — one save, so an
/// audit is never Issued with half its actions minted.</summary>
public sealed class IssueHsAuditHandler : ICommandHandler<IssueHsAudit, HsAuditView>
{
    private readonly JpmsContext context;
    public IssueHsAuditHandler(JpmsContext context) { this.context = context; }

    public async Task<HsAuditView> HandleAsync(IssueHsAudit command, CancellationToken cancellationToken)
    {
        var audit = await context.HsAudits.FirstOrDefaultAsync(row => row.HsAuditId == command.HsAuditId, cancellationToken)
            ?? throw new InvalidOperationException("That audit no longer exists.");
        if (audit.Status != (int)HsAuditStatus.Draft)
            throw new InvalidOperationException($"Only a draft audit can be issued — this one is {((HsAuditStatus)audit.Status).DisplayName()}.");

        var items = await context.HsAuditItems
            .Where(row => row.HsAuditId == command.HsAuditId)
            .OrderBy(row => row.DisplayOrder)
            .ToListAsync(cancellationToken);
        if (!items.Any(item => item.Rate is not null))
            throw new InvalidOperationException("Rate at least one item before issuing the audit.");

        var issuedAt = DateTimeOffset.UtcNow;
        foreach (var finding in items.Where(HsAuditCorrectiveActions.IsUnlinkedFinding))
        {
            var correctiveAction = HsAuditCorrectiveActions.Mint(finding, audit.ProjectId, issuedAt);
            context.HsRecords.Add(correctiveAction);
            finding.HsRecordId = correctiveAction.HsRecordId;
        }

        audit.Score = HsAuditRules.ScoreOf(items);
        audit.Status = (int)HsAuditStatus.Issued;
        audit.IssuedAt = issuedAt;

        await context.SaveChangesAsync(cancellationToken);
        return new HsAuditView(audit.ToModel(), items.Select(item => item.ToModel()).ToList());
    }
}

public sealed class IssueHsAuditAuthorisation
{
    public bool Allows(SignedInUser user, IssueHsAudit command) => HsAuditRoles.Auditors.IncludesAny(user.Roles);
}

using Jewel.JPMS.Contracts.Hs;

namespace Jewel.JPMS.Api.Features.Hs.Audits.Commands;

/// <summary>The manager's declaration that the actions are done. Issued → Closed, refused while
/// any corrective action minted from this audit is still open — the declaration is a fact the
/// register has to agree with.</summary>
public sealed class CloseHsAuditHandler : ICommandHandler<CloseHsAudit, HsAudit>
{
    private readonly JpmsContext context;
    public CloseHsAuditHandler(JpmsContext context) { this.context = context; }

    public async Task<HsAudit> HandleAsync(CloseHsAudit command, CancellationToken cancellationToken)
    {
        var audit = await context.HsAudits.FirstOrDefaultAsync(row => row.HsAuditId == command.HsAuditId, cancellationToken)
            ?? throw new InvalidOperationException("That audit no longer exists.");
        if (audit.Status != (int)HsAuditStatus.Issued)
            throw new InvalidOperationException($"Only an issued audit can be closed — this one is {((HsAuditStatus)audit.Status).DisplayName()}.");

        var openActions = await OpenActionCountAsync(command.HsAuditId, cancellationToken);
        if (openActions > 0)
            throw new InvalidOperationException($"{openActions} corrective action{(openActions == 1 ? " is" : "s are")} still open on this audit — close them on the register first.");

        audit.ManagerName = command.ManagerName.Trim();
        audit.Status = (int)HsAuditStatus.Closed;
        audit.ClosedAt = DateTimeOffset.UtcNow;
        await context.SaveChangesAsync(cancellationToken);
        return audit.ToModel();
    }

    private async Task<int> OpenActionCountAsync(string hsAuditId, CancellationToken cancellationToken)
    {
        var linkedRecordIds = await context.HsAuditItems
            .Where(row => row.HsAuditId == hsAuditId && row.HsRecordId != null)
            .Select(row => row.HsRecordId!)
            .ToListAsync(cancellationToken);
        if (linkedRecordIds.Count == 0) return 0;
        return await context.HsRecords
            .CountAsync(row => linkedRecordIds.Contains(row.HsRecordId) && row.Status != (int)HsStatus.Closed, cancellationToken);
    }
}

public sealed class CloseHsAuditAuthorisation
{
    public bool Allows(SignedInUser user, CloseHsAudit command) => HsAuditRoles.Auditors.IncludesAny(user.Roles);
}

public sealed class CloseHsAuditValidation
{
    public ValidationOutcome Check(CloseHsAudit command)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(command.HsAuditId)) errors.Add("HsAuditId is required.");
        if (string.IsNullOrWhiteSpace(command.ManagerName)) errors.Add("Name the manager making the declaration.");
        return errors.Count == 0 ? ValidationOutcome.Passed : new ValidationOutcome(errors);
    }
}

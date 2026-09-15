using Jewel.JPMS.Contracts.Hs;

namespace Jewel.JPMS.Api.Features.Hs.Audits.Commands;

public sealed class UpdateHsAuditDetailsHandler : ICommandHandler<UpdateHsAuditDetails, HsAudit>
{
    private readonly JpmsContext context;
    public UpdateHsAuditDetailsHandler(JpmsContext context) { this.context = context; }

    public async Task<HsAudit> HandleAsync(UpdateHsAuditDetails command, CancellationToken cancellationToken)
    {
        var audit = await context.HsAudits.FirstOrDefaultAsync(row => row.HsAuditId == command.HsAuditId, cancellationToken)
            ?? throw new InvalidOperationException("That audit no longer exists.");
        if (!HsAuditRules.IsEditable(audit)) throw new InvalidOperationException("A closed audit can't be edited.");

        HsAuditRules.Apply(audit, command.Details);
        await context.SaveChangesAsync(cancellationToken);
        return audit.ToModel();
    }
}

public sealed class UpdateHsAuditDetailsAuthorisation
{
    public bool Allows(SignedInUser user, UpdateHsAuditDetails command) => HsAuditRoles.Auditors.IncludesAny(user.Roles);
}

public sealed class UpdateHsAuditDetailsValidation
{
    public ValidationOutcome Check(UpdateHsAuditDetails command)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(command.HsAuditId)) errors.Add("HsAuditId is required.");
        errors.AddRange(HsAuditRules.DetailProblems(command.Details));
        return errors.Count == 0 ? ValidationOutcome.Passed : new ValidationOutcome(errors);
    }
}

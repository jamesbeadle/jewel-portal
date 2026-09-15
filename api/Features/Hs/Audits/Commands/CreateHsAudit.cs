using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Contracts.Hs;

namespace Jewel.JPMS.Api.Features.Hs.Audits.Commands;

/// <summary>Plants an audit: the header, the next HSA number on the project, the last issued
/// audit's score as PreviousScore, and every item of the current framework blank.</summary>
public sealed class CreateHsAuditHandler : ICommandHandler<CreateHsAudit, HsAudit>
{
    private readonly JpmsContext context;
    public CreateHsAuditHandler(JpmsContext context) { this.context = context; }

    public async Task<HsAudit> HandleAsync(CreateHsAudit command, CancellationToken cancellationToken)
    {
        var projectExists = await context.Projects.AnyAsync(project => project.ProjectId == command.ProjectId, cancellationToken);
        if (!projectExists) throw new InvalidOperationException($"Project '{command.ProjectId}' not found.");

        var audit = new HsAuditEntity
        {
            HsAuditId = HsIdentifierFactory.NextHsAuditId(),
            ProjectId = command.ProjectId,
            Number = await NextNumberAsync(command.ProjectId, cancellationToken),
            Status = (int)HsAuditStatus.Draft,
            PreviousScore = await LastIssuedScoreAsync(command.ProjectId, cancellationToken),
            TemplateVersion = HsAuditTemplate.Version,
            CreatedByEmail = command.CreatedByEmail,
            CreatedAt = DateTimeOffset.UtcNow
        };
        HsAuditRules.Apply(audit, command.Details);
        context.HsAudits.Add(audit);
        context.HsAuditItems.AddRange(PlantedItems(audit.HsAuditId));

        await context.SaveChangesAsync(cancellationToken);
        return audit.ToModel();
    }

    // Per project, max + 1 — the work-order rule: a deleted row never re-issues a number.
    private async Task<int> NextNumberAsync(string projectId, CancellationToken cancellationToken) =>
        (await context.HsAudits.Where(row => row.ProjectId == projectId).MaxAsync(row => (int?)row.Number, cancellationToken) ?? 0) + 1;

    private async Task<decimal?> LastIssuedScoreAsync(string projectId, CancellationToken cancellationToken) =>
        await context.HsAudits.AsNoTracking()
            .Where(row => row.ProjectId == projectId && row.IssuedAt != null)
            .OrderByDescending(row => row.IssuedAt)
            .Select(row => row.Score)
            .FirstOrDefaultAsync(cancellationToken);

    private static IEnumerable<HsAuditItemEntity> PlantedItems(string hsAuditId) =>
        HsAuditTemplate.Items.Select((item, index) => new HsAuditItemEntity
        {
            HsAuditItemId = HsIdentifierFactory.NextHsAuditItemId(),
            HsAuditId = hsAuditId,
            Code = item.Code,
            Section = item.Section,
            Name = item.Name,
            DisplayOrder = index + 1
        });
}

public sealed class CreateHsAuditAuthorisation
{
    public bool Allows(SignedInUser user, CreateHsAudit command) => HsAuditRoles.Auditors.IncludesAny(user.Roles);
}

public sealed class CreateHsAuditValidation
{
    public ValidationOutcome Check(CreateHsAudit command)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(command.ProjectId)) errors.Add("ProjectId is required.");
        errors.AddRange(HsAuditRules.DetailProblems(command.Details));
        return errors.Count == 0 ? ValidationOutcome.Passed : new ValidationOutcome(errors);
    }
}

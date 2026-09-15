using Jewel.JPMS.Contracts.Hs;

namespace Jewel.JPMS.Api.Features.Hs.Audits.Commands;

/// <summary>Writes the named items wholesale and recomputes the audit's score from every item.
/// Items are written on a Draft or an Issued audit (the date rectified lands after issue); a
/// Closed audit is history.</summary>
public sealed class UpdateHsAuditItemsHandler : ICommandHandler<UpdateHsAuditItems, HsAuditView>
{
    private readonly JpmsContext context;
    public UpdateHsAuditItemsHandler(JpmsContext context) { this.context = context; }

    public async Task<HsAuditView> HandleAsync(UpdateHsAuditItems command, CancellationToken cancellationToken)
    {
        var audit = await context.HsAudits.FirstOrDefaultAsync(row => row.HsAuditId == command.HsAuditId, cancellationToken)
            ?? throw new InvalidOperationException("That audit no longer exists.");
        if (!HsAuditRules.IsEditable(audit)) throw new InvalidOperationException("A closed audit can't be edited.");

        var items = await context.HsAuditItems
            .Where(row => row.HsAuditId == command.HsAuditId)
            .OrderBy(row => row.DisplayOrder)
            .ToListAsync(cancellationToken);
        var itemsById = items.ToDictionary(item => item.HsAuditItemId);

        foreach (var entry in command.Items)
        {
            if (!itemsById.TryGetValue(entry.HsAuditItemId, out var item))
                throw new InvalidOperationException($"Item '{entry.HsAuditItemId}' is not on this audit.");
            HsAuditRules.Apply(item, entry);
        }
        audit.Score = HsAuditRules.ScoreOf(items);

        await context.SaveChangesAsync(cancellationToken);
        return new HsAuditView(audit.ToModel(), items.Select(item => item.ToModel()).ToList());
    }
}

public sealed class UpdateHsAuditItemsAuthorisation
{
    public bool Allows(SignedInUser user, UpdateHsAuditItems command) => HsAuditRoles.Auditors.IncludesAny(user.Roles);
}

public sealed class UpdateHsAuditItemsValidation
{
    public ValidationOutcome Check(UpdateHsAuditItems command)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(command.HsAuditId)) errors.Add("HsAuditId is required.");
        errors.AddRange(HsAuditRules.ItemProblems(command.Items ?? Array.Empty<HsAuditItemEntry>()));
        return errors.Count == 0 ? ValidationOutcome.Passed : new ValidationOutcome(errors);
    }
}

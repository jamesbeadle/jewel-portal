using Jewel.JPMS.Api.Data.StoredValues;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Data;

/// <summary>
/// No value reaches SQL Server that its column cannot hold. Every save checks what it is about to
/// write against the model first and refuses with the sentence a person can act on — "Description
/// on the valuation line item is 550 characters long; it can hold at most 512" — instead of the
/// truncation error that surfaced as "Backend call failure" (V33, 2026-09-24).
/// </summary>
public sealed partial class JpmsContext
{
    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        RefuseValuesTheColumnsCannotHold();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        RefuseValuesTheColumnsCannotHold();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    private void RefuseValuesTheColumnsCannotHold()
    {
        ChangeTracker.DetectChanges();
        var problems = StoredValueChecks.ProblemsIn(ChangeTracker.Entries());
        if (problems.Count > 0) throw new StoredValuesRejectedException(problems);
    }
}

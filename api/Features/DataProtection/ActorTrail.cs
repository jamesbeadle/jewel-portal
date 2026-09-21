namespace Jewel.JPMS.Api.Features.DataProtection;

/// <summary>
/// The two append-only registers that name who acted — the audit trail and the agent activity
/// log — outlive the person: a deleted sign-in must not leave its address on ten thousand rows.
/// The actor is rewritten to the person's pseudonym in one statement per register, so the
/// register still shows one actor did those things while no longer saying who.
/// </summary>
public static class ActorTrail
{
    public static async Task<int> PseudonymiseAsync(JpmsContext context, string email, CancellationToken cancellationToken)
    {
        var pseudonym = PersonPseudonym.For(email);
        var auditRows = await context.AuditEvents
            .Where(row => row.ActorEmail == email)
            .ExecuteUpdateAsync(set => set.SetProperty(row => row.ActorEmail, pseudonym), cancellationToken);
        var activityRows = await context.AgentActivity
            .Where(row => row.ActorEmail == email)
            .ExecuteUpdateAsync(set => set.SetProperty(row => row.ActorEmail, pseudonym), cancellationToken);
        return auditRows + activityRows;
    }
}

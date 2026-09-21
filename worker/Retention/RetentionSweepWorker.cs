using Jewel.JPMS.Api.Data;
using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Worker.Retention;

/// <summary>
/// The nightly sweep that keeps the stores of spent credentials and cached text from growing
/// forever — the retention the schema anticipated ("expired-row cleanup scans by expiry",
/// JpmsContext.Model) and nothing ran (security review, 2026-09-21). Deletes are by set, so a
/// table of any size sweeps in one statement each; nothing here touches a row a person could
/// still use.
/// </summary>
public sealed class RetentionSweepWorker
{
    private readonly JpmsContext db;
    private readonly ILogger<RetentionSweepWorker> logger;

    public RetentionSweepWorker(JpmsContext db, ILogger<RetentionSweepWorker> logger)
    {
        this.db = db;
        this.logger = logger;
    }

    [Function(nameof(RetentionSweepWorker))]
    public async Task Run([TimerTrigger("0 45 3 * * *")] TimerInfo timer, CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var spentBefore = now - RetentionPeriods.SpentCredentials;
        var sessions = await db.UserSessions
            .Where(row => row.ExpiresAt < spentBefore || row.RevokedAt < spentBefore)
            .ExecuteDeleteAsync(cancellationToken);
        var resetTokens = await db.PasswordResetTokens
            .Where(row => row.ExpiresAt < spentBefore || row.ConsumedAt < spentBefore)
            .ExecuteDeleteAsync(cancellationToken);
        var authCodes = await db.OAuthAuthCodes
            .Where(row => row.ExpiresAt < spentBefore || row.UsedAt < spentBefore)
            .ExecuteDeleteAsync(cancellationToken);
        var oauthTokens = await db.OAuthTokens
            .Where(row => row.ExpiresAt < spentBefore || row.RevokedAt < spentBefore)
            .ExecuteDeleteAsync(cancellationToken);
        var accessRequests = await db.AccessRequests
            .Where(row => row.RequestedAt < now - RetentionPeriods.UnansweredAccessRequests)
            .ExecuteDeleteAsync(cancellationToken);
        var scans = await db.DocumentOcrResults
            .Where(row => row.CreatedAtUtc < now - RetentionPeriods.ScanText)
            .ExecuteDeleteAsync(cancellationToken);
        var auditRows = await db.AuditEvents
            .Where(row => row.OccurredAt < now - RetentionPeriods.AuditTrail)
            .ExecuteDeleteAsync(cancellationToken);
        var activityRows = await db.AgentActivity
            .Where(row => row.OccurredAt < now - RetentionPeriods.AgentActivity)
            .ExecuteDeleteAsync(cancellationToken);

        logger.LogInformation(
            "Retention sweep: {Sessions} spent session(s), {ResetTokens} spent invite/reset token(s), {AuthCodes} spent OAuth code(s), {OAuthTokens} spent OAuth token(s), {AccessRequests} unanswered access request(s), {Scans} cached scan text(s), {AuditRows} audit event(s) and {ActivityRows} agent activity row(s) past retention removed.",
            sessions, resetTokens, authCodes, oauthTokens, accessRequests, scans, auditRows, activityRows);
    }
}

using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Features.Forms.Mail;
using Jewel.JPMS.Api.Features.Forms;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Hs.Notifications;

/// <summary>How a run went: the digests sent, and the events spent (told, or with nobody to tell).</summary>
public sealed record HsNotificationOutcome(int DigestsSent, int EventsSpent);

/// <summary>
/// The sweep the worker runs every few minutes: reads the events not yet told, asks the planner
/// which sittings are over and who hears of them, sends each digest through the same mailer every
/// portal email leaves by, and stamps the events told. An email that will not go is logged and the
/// events stay unsent for the next run; a run that finds nothing settled does nothing.
/// </summary>
public sealed class HsNotificationSweep
{
    private readonly JpmsContext context;
    private readonly IFormMailer mailer;
    private readonly FormSiteOptions options;
    private readonly ILogger<HsNotificationSweep> logger;

    public HsNotificationSweep(JpmsContext context, IFormMailer mailer, FormSiteOptions options, ILogger<HsNotificationSweep> logger)
    {
        this.context = context;
        this.mailer = mailer;
        this.options = options;
        this.logger = logger;
    }

    public async Task<HsNotificationOutcome> RunAsync(DateTimeOffset now, CancellationToken cancellationToken)
    {
        var unsent = await context.HsRecordEvents.Where(row => row.NotifiedAt == null).ToListAsync(cancellationToken);
        var settledProjectIds = HsNotificationDigests.SettledProjectIds(unsent, now).ToList();
        if (settledProjectIds.Count == 0) return new HsNotificationOutcome(0, 0);
        if (!mailer.IsConfigured) return NothingWhileMailIsOff();
        var recipients = await RecipientsAsync(settledProjectIds, cancellationToken);
        var digests = HsNotificationDigests.Plan(unsent, recipients, now);
        var sent = await SendAsync(digests, cancellationToken);
        var spent = SpendSettledEvents(unsent, settledProjectIds, digests, sent, now);
        await context.SaveChangesAsync(cancellationToken);
        return new HsNotificationOutcome(sent.Count, spent);
    }

    private HsNotificationOutcome NothingWhileMailIsOff()
    {
        logger.LogWarning("H&S digest: email is not configured (CommunicationServicesConnectionString), so the settled events wait.");
        return new HsNotificationOutcome(0, 0);
    }

    private async Task<Dictionary<string, HsDigestRecipients>> RecipientsAsync(IReadOnlyList<string> projectIds, CancellationToken cancellationToken)
    {
        var officers = await context.DirectoryUserRoles.AsNoTracking()
            .Where(row => row.Role == (int)Role.HealthSafetyOfficer)
            .Select(row => row.DirectoryUserEmail)
            .ToListAsync(cancellationToken);
        var projects = await context.Projects.AsNoTracking()
            .Where(project => projectIds.Contains(project.ProjectId))
            .Select(project => new { project.ProjectId, project.SiteManagerEmail })
            .ToListAsync(cancellationToken);
        return projects.ToDictionary(project => project.ProjectId, project => new HsDigestRecipients(project.SiteManagerEmail, officers));
    }

    private async Task<List<HsDigest>> SendAsync(IReadOnlyList<HsDigest> digests, CancellationToken cancellationToken)
    {
        var sent = new List<HsDigest>();
        foreach (var digest in digests)
        {
            var email = HsDigestEmails.Compose(digest, await HsDigestContexts.ReadAsync(context, options, digest.ProjectId, cancellationToken));
            var isSent = await SendQuietlyAsync(email, cancellationToken);
            if (isSent) sent.Add(digest);
        }
        return sent;
    }

    private async Task<bool> SendQuietlyAsync(FormEmail email, CancellationToken cancellationToken)
    {
        try { await mailer.SendAsync(email, cancellationToken); return true; }
        catch (Exception failure) when (failure is not OperationCanceledException)
        {
            logger.LogWarning(failure, "H&S digest: an email to {To} was not delivered; its events stay unsent.", email.To.First());
            return false;
        }
    }

    /// <summary>A settled project's events are spent once every digest planned for it went — or when none was
    /// planned, because nobody needed telling. A project with a digest that would not go keeps its events for the next run.</summary>
    private static int SpendSettledEvents(
        IReadOnlyList<HsRecordEventEntity> unsent, IReadOnlyList<string> settledProjectIds,
        IReadOnlyList<HsDigest> planned, IReadOnlyList<HsDigest> sent, DateTimeOffset now)
    {
        var undelivered = planned.Except(sent).Select(digest => digest.ProjectId).ToHashSet(StringComparer.Ordinal);
        var told = settledProjectIds.Where(projectId => !undelivered.Contains(projectId)).ToHashSet(StringComparer.Ordinal);
        var spent = unsent.Where(occurrence => told.Contains(occurrence.ProjectId)).ToList();
        foreach (var occurrence in spent) occurrence.NotifiedAt = now;
        return spent.Count;
    }
}

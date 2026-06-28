using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Features.MailboxIntake.Actions;
using Jewel.JPMS.Api.Features.MailboxIntake.Graph;
using Jewel.JPMS.Contracts.Requests;
using Jewel.JPMS.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Jewel.JPMS.Api.Features.MailboxIntake.Ingestion;

/// <summary>
/// Keeps the triage queue a true mirror of the Inbox. The delta sweep only ever adds emails; this
/// pass reconciles in the other direction by comparing the whole Inbox against the intake table:
///
///  - A NeedsTriage email no longer in the Inbox (a human deleted/moved it) → RemovedFromMailbox,
///    so it drops out of triage. (Reconcile-only: the message has already left the Inbox, so it is
///    invisible in both Inbox and queue; we don't chase the stray to file it.)
///  - A RemovedFromMailbox email that reappears in the Inbox → back to NeedsTriage.
///  - An already-triaged email (Claimed/Linked/Discarded/Failed) still sitting in the Inbox means
///    its outcome move never landed → re-drive the move. This self-heals the "linked but not filed"
///    case.
///  - Any row whose stored Graph id is stale versus the Inbox → refresh it.
///
/// The DB change is the guarantee; the folder moves are best-effort, consistent with "DB is the
/// source of truth, the mailbox folder is a mirror". Runs on a 5-minute cadence, offset from the
/// delta sweep so the two timers don't collide.
/// </summary>
public sealed class MailboxInboxReconcile
{
    private readonly MailboxIntakeOptions _options;
    private readonly IGraphMailClient _graph;
    private readonly JpmsContext _context;
    private readonly IMailboxActionScheduler _scheduler;
    private readonly ILogger<MailboxInboxReconcile> _logger;

    public MailboxInboxReconcile(
        MailboxIntakeOptions options,
        IGraphMailClient graph,
        JpmsContext context,
        IMailboxActionScheduler scheduler,
        ILogger<MailboxInboxReconcile> logger)
    {
        _options = options;
        _graph = graph;
        _context = context;
        _scheduler = scheduler;
        _logger = logger;
    }

    [Function(nameof(MailboxInboxReconcile))]
    public async Task Run([TimerTrigger("0 2-59/5 * * * *")] TimerInfo timer, CancellationToken ct)
    {
        if (!_options.Enabled || !_options.EnableReconcile || !_options.IsConfigured)
            return;

        // 1. Snapshot the Inbox: internetMessageId -> current Graph id. internetMessageId is unique
        //    per message, but TryAdd guards against any pathological duplicate without throwing.
        var inbox = await _graph.ListInboxMessageIdentitiesAsync(ct);
        var inboxByImid = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var item in inbox)
            inboxByImid.TryAdd(item.InternetMessageId, item.Id);

        // 2. Load the rows reconciliation can possibly act on: every un-triaged row (to catch
        //    ghosts) plus every row whose email is currently in the Inbox (to refresh ids, reactivate
        //    removed-then-returned emails, and re-drive stuck moves).
        var inboxImids = inboxByImid.Keys.ToList();
        var entities = await _context.IntakeEmails
            .Where(e => e.Status == (int)IntakeStatus.NeedsTriage || inboxImids.Contains(e.InternetMessageId))
            .ToListAsync(ct);

        var rows = entities.Select(e =>
            new IntakeReconcileRow(e.IntakeId, e.InternetMessageId, e.GraphMessageId, (IntakeStatus)e.Status));

        var plan = InboxReconciliation.Plan(inboxByImid, rows);

        if (plan.RetireIntakeIds.Count == 0 && plan.ReactivateIntakeIds.Count == 0
            && plan.RedriveMoves.Count == 0 && plan.RefreshedGraphIds.Count == 0)
        {
            return;
        }

        // 3. Apply the DB-side changes (the guarantee).
        var byId = entities.ToDictionary(e => e.IntakeId, StringComparer.Ordinal);

        foreach (var id in plan.RetireIntakeIds)
            if (byId.TryGetValue(id, out var e))
                e.Status = (int)IntakeStatus.RemovedFromMailbox;

        foreach (var id in plan.ReactivateIntakeIds)
            if (byId.TryGetValue(id, out var e))
                e.Status = (int)IntakeStatus.NeedsTriage;

        foreach (var (id, graphId) in plan.RefreshedGraphIds)
            if (byId.TryGetValue(id, out var e))
                e.GraphMessageId = graphId;

        await _context.SaveChangesAsync(ct);

        // 4. Best-effort: re-drive outcome moves for already-triaged emails still stuck in the Inbox.
        foreach (var move in plan.RedriveMoves)
            await _scheduler.ScheduleOutcomeMoveAsync(move.IntakeId, move.Status, ct);

        _logger.LogInformation(
            "Mailbox reconcile: {Retired} retired, {Reactivated} reactivated, {Redriven} move(s) re-driven, {Refreshed} id(s) refreshed (Inbox size {InboxCount}).",
            plan.RetireIntakeIds.Count, plan.ReactivateIntakeIds.Count, plan.RedriveMoves.Count,
            plan.RefreshedGraphIds.Count, inboxByImid.Count);
    }
}

using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Requests;

// A single intake row, reduced to just what reconciliation needs to reason about.
public sealed record IntakeReconcileRow(
    string IntakeId,
    string InternetMessageId,
    string? GraphMessageId,
    IntakeStatus Status);

// "This intake (currently at Status) is still sitting in the Inbox but the app already
// triaged it — re-drive its outcome move." Status is carried so the caller can resolve the
// right destination folder via OutcomeFolders.
public sealed record IntakeReconcileMove(string IntakeId, IntakeStatus Status);

// The full set of changes reconciliation wants applied, computed purely from the current Inbox
// contents and the current intake rows. The caller applies these against the DB / mailbox.
//   RetireIntakeIds     – NeedsTriage rows no longer in the Inbox → set RemovedFromMailbox.
//   ReactivateIntakeIds – RemovedFromMailbox rows that reappeared in the Inbox → set NeedsTriage.
//   RedriveMoves        – already-triaged rows still stuck in the Inbox → re-schedule their move.
//   RefreshedGraphIds   – IntakeId → current Inbox Graph id, for rows whose stored id is stale.
public sealed record InboxReconcilePlan(
    IReadOnlyList<string> RetireIntakeIds,
    IReadOnlyList<string> ReactivateIntakeIds,
    IReadOnlyList<IntakeReconcileMove> RedriveMoves,
    IReadOnlyDictionary<string, string> RefreshedGraphIds);

// Pure reconciliation between the Inbox (the source of truth) and the intake table.
//
// The Inbox is keyed by InternetMessageId (stable across moves) → its current Graph id. We
// compare each intake row against that map and decide what should change. This is deliberately
// side-effect free so it can be unit-tested without Graph or a database.
public static class InboxReconciliation
{
    public static InboxReconcilePlan Plan(
        IReadOnlyDictionary<string, string> inboxByInternetMessageId,
        IEnumerable<IntakeReconcileRow> rows)
    {
        var retire = new List<string>();
        var reactivate = new List<string>();
        var redrive = new List<IntakeReconcileMove>();
        var refreshed = new Dictionary<string, string>(StringComparer.Ordinal);

        foreach (var row in rows)
        {
            string? currentGraphId = null;
            var inInbox = !string.IsNullOrEmpty(row.InternetMessageId)
                && inboxByInternetMessageId.TryGetValue(row.InternetMessageId, out currentGraphId);

            switch (row.Status)
            {
                case IntakeStatus.NeedsTriage:
                    if (!inInbox)
                    {
                        // The one case that means a human removed an un-triaged email: retire it.
                        retire.Add(row.IntakeId);
                    }
                    else if (!string.Equals(row.GraphMessageId, currentGraphId, StringComparison.Ordinal))
                    {
                        refreshed[row.IntakeId] = currentGraphId!;
                    }
                    break;

                case IntakeStatus.RemovedFromMailbox:
                    if (inInbox)
                    {
                        // It came back (human moved it into the Inbox again) → put it back in triage.
                        reactivate.Add(row.IntakeId);
                        if (!string.Equals(row.GraphMessageId, currentGraphId, StringComparison.Ordinal))
                        {
                            refreshed[row.IntakeId] = currentGraphId!;
                        }
                    }
                    break;

                case IntakeStatus.Claimed:
                case IntakeStatus.Linked:
                case IntakeStatus.Discarded:
                case IntakeStatus.Failed:
                    // These were triaged; the app should have moved them out of the Inbox. If one is
                    // still here, an earlier outcome move never landed — re-drive it. Refresh the id
                    // first so the move targets the message at its current Inbox id.
                    if (inInbox)
                    {
                        if (!string.Equals(row.GraphMessageId, currentGraphId, StringComparison.Ordinal))
                        {
                            refreshed[row.IntakeId] = currentGraphId!;
                        }
                        redrive.Add(new IntakeReconcileMove(row.IntakeId, row.Status));
                    }
                    break;
            }
        }

        return new InboxReconcilePlan(retire, reactivate, redrive, refreshed);
    }
}

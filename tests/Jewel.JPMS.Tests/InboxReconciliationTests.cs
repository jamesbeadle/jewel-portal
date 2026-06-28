using Jewel.JPMS.Contracts.Requests;
using Jewel.JPMS.Models;
using Xunit;

namespace Jewel.JPMS.Tests;

// The reconciliation planner is the brain that keeps the triage queue a mirror of the Inbox. These
// tests pin its decisions: what gets retired, reactivated, re-driven, and id-refreshed, purely from
// the Inbox snapshot (internetMessageId -> current Graph id) and the current intake rows.
public sealed class InboxReconciliationTests
{
    private static Dictionary<string, string> Inbox(params (string Imid, string GraphId)[] items)
    {
        var d = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var (imid, graphId) in items)
            d[imid] = graphId;
        return d;
    }

    [Fact]
    public void NeedsTriage_goneFromInbox_isRetired()
    {
        var inbox = Inbox(); // empty: the email was removed by a human
        var rows = new[] { new IntakeReconcileRow("INT-1", "imid-1", "graph-1", IntakeStatus.NeedsTriage) };

        var plan = InboxReconciliation.Plan(inbox, rows);

        Assert.Equal(new[] { "INT-1" }, plan.RetireIntakeIds);
        Assert.Empty(plan.ReactivateIntakeIds);
        Assert.Empty(plan.RedriveMoves);
        Assert.Empty(plan.RefreshedGraphIds);
    }

    [Fact]
    public void Claimed_goneFromInbox_isLeftAlone()
    {
        // Claimed/Linked/Discarded left the Inbox by the app's OWN move — they must not be retired.
        var inbox = Inbox();
        var rows = new[] { new IntakeReconcileRow("INT-2", "imid-2", "graph-2", IntakeStatus.Claimed) };

        var plan = InboxReconciliation.Plan(inbox, rows);

        Assert.Empty(plan.RetireIntakeIds);
        Assert.Empty(plan.ReactivateIntakeIds);
        Assert.Empty(plan.RedriveMoves);
        Assert.Empty(plan.RefreshedGraphIds);
    }

    [Fact]
    public void RemovedFromMailbox_reappearsInInbox_isReactivatedAndRefreshed()
    {
        var inbox = Inbox(("imid-3", "graph-3-new")); // human moved it back into the Inbox
        var rows = new[] { new IntakeReconcileRow("INT-3", "imid-3", "graph-3-old", IntakeStatus.RemovedFromMailbox) };

        var plan = InboxReconciliation.Plan(inbox, rows);

        Assert.Equal(new[] { "INT-3" }, plan.ReactivateIntakeIds);
        Assert.Equal("graph-3-new", plan.RefreshedGraphIds["INT-3"]);
        Assert.Empty(plan.RetireIntakeIds);
        Assert.Empty(plan.RedriveMoves);
    }

    [Fact]
    public void Linked_stillInInbox_redrivesTheMove()
    {
        // The app linked it but the outcome move never landed — it's stuck in the Inbox. Re-drive it.
        var inbox = Inbox(("imid-4", "graph-4"));
        var rows = new[] { new IntakeReconcileRow("INT-4", "imid-4", "graph-4", IntakeStatus.Linked) };

        var plan = InboxReconciliation.Plan(inbox, rows);

        var move = Assert.Single(plan.RedriveMoves);
        Assert.Equal("INT-4", move.IntakeId);
        Assert.Equal(IntakeStatus.Linked, move.Status);
        Assert.Empty(plan.RetireIntakeIds);
        Assert.Empty(plan.ReactivateIntakeIds);
    }

    [Fact]
    public void Linked_stillInInbox_withStaleId_redrivesAndRefreshes()
    {
        var inbox = Inbox(("imid-5", "graph-5-current"));
        var rows = new[] { new IntakeReconcileRow("INT-5", "imid-5", "graph-5-stale", IntakeStatus.Linked) };

        var plan = InboxReconciliation.Plan(inbox, rows);

        Assert.Single(plan.RedriveMoves);
        Assert.Equal("graph-5-current", plan.RefreshedGraphIds["INT-5"]);
    }

    [Fact]
    public void NeedsTriage_inInbox_withStaleId_isRefreshedOnly()
    {
        var inbox = Inbox(("imid-6", "graph-6-current"));
        var rows = new[] { new IntakeReconcileRow("INT-6", "imid-6", "graph-6-stale", IntakeStatus.NeedsTriage) };

        var plan = InboxReconciliation.Plan(inbox, rows);

        Assert.Equal("graph-6-current", plan.RefreshedGraphIds["INT-6"]);
        Assert.Empty(plan.RetireIntakeIds);
        Assert.Empty(plan.ReactivateIntakeIds);
        Assert.Empty(plan.RedriveMoves);
    }

    [Fact]
    public void NeedsTriage_inInbox_withFreshId_isNoOp()
    {
        var inbox = Inbox(("imid-7", "graph-7"));
        var rows = new[] { new IntakeReconcileRow("INT-7", "imid-7", "graph-7", IntakeStatus.NeedsTriage) };

        var plan = InboxReconciliation.Plan(inbox, rows);

        Assert.Empty(plan.RetireIntakeIds);
        Assert.Empty(plan.ReactivateIntakeIds);
        Assert.Empty(plan.RedriveMoves);
        Assert.Empty(plan.RefreshedGraphIds);
    }

    [Fact]
    public void MixedBatch_classifiesEachRowIndependently()
    {
        var inbox = Inbox(
            ("imid-keep", "g-keep"),       // NeedsTriage, present, fresh -> no-op
            ("imid-back", "g-back-new"),   // RemovedFromMailbox, reappeared -> reactivate
            ("imid-stuck", "g-stuck"));    // Linked, still here -> redrive
        var rows = new[]
        {
            new IntakeReconcileRow("INT-keep", "imid-keep", "g-keep", IntakeStatus.NeedsTriage),
            new IntakeReconcileRow("INT-ghost", "imid-ghost", "g-ghost", IntakeStatus.NeedsTriage), // gone -> retire
            new IntakeReconcileRow("INT-back", "imid-back", "g-back-old", IntakeStatus.RemovedFromMailbox),
            new IntakeReconcileRow("INT-stuck", "imid-stuck", "g-stuck", IntakeStatus.Linked),
        };

        var plan = InboxReconciliation.Plan(inbox, rows);

        Assert.Equal(new[] { "INT-ghost" }, plan.RetireIntakeIds);
        Assert.Equal(new[] { "INT-back" }, plan.ReactivateIntakeIds);
        Assert.Equal("INT-stuck", Assert.Single(plan.RedriveMoves).IntakeId);
        Assert.Equal("g-back-new", plan.RefreshedGraphIds["INT-back"]);
    }
}

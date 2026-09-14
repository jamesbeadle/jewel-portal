using Jewel.JPMS.Contracts.Cqrs;

namespace Jewel.JPMS.Contracts.Procurement;

/// <summary>
/// One-off migration pass (2026-09-14): moves every work order's mailbox correspondence from the
/// legacy flat workflow tag ("JPMS/WO-0045") onto the project-qualified one
/// ("JPMS/JBB-2026-001-WO-0045"). Order numbers are minted per project and the migrated
/// Buildertrend orders keep theirs, so a flat tag can name an order on two projects — By France's
/// Farrant order and Coombe Lane's Hamilton Glass order were both "WO-0045". Where one order carries
/// the number the whole tag moves; where several do, each conversation follows the order the audit
/// trail says it was linked to, and a conversation the trail cannot place is LEFT on the legacy tag
/// and named in the summary for a person to file — nothing is guessed. Idempotent: a second run
/// finds nothing under the old tags but what it left.
/// </summary>
public sealed record RetagWorkOrderWorkflowTags() : ICommand<WorkOrderRetagSummary>;

/// <summary>Outcome of a retag pass. Failures are logged per tag and never abort the sweep.
/// LeftForAPerson names each thread still on a colliding legacy tag, with the orders it could be.</summary>
public sealed record WorkOrderRetagSummary(
    int TagsProcessed,
    int EmailsMoved,
    int Failures,
    IReadOnlyList<WorkOrderRetagLeftover> LeftForAPerson);

/// <summary>A thread the sweep would not guess at: the legacy tag it still carries, its subject,
/// and the qualified tags (one per project's order) a person chooses between.</summary>
public sealed record WorkOrderRetagLeftover(string LegacyTag, string Subject, IReadOnlyList<string> CandidateTags);

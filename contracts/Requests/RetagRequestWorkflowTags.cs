using Jewel.JPMS.Contracts.Cqrs;

namespace Jewel.JPMS.Contracts.Requests;

/// <summary>
/// One-off migration pass: moves every request's mailbox correspondence from the legacy flat workflow
/// tag ("JPMS/RFI-012") onto the project-qualified one ("JPMS/JBB-2026-001-RFI-012"). Flat tags collide
/// once two projects carry the same reference, so all live tagging is now project-qualified; this
/// command brings historic mail in line. Idempotent — a second run finds nothing under the old tags.
/// </summary>
public sealed record RetagRequestWorkflowTags() : ICommand<RequestRetagSummary>;

/// <summary>Outcome of a retag pass. Failures are logged per request and never abort the sweep.</summary>
public sealed record RequestRetagSummary(int RequestsProcessed, int EmailsMoved, int Failures);

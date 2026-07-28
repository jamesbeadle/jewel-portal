using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Commercial;

// Drafts the valuation-report email in the shared mailbox — nothing is sent; a human reviews and
// sends it from Outlook, matching the subcontractor-statement and work-order convention. The
// snapshot is the only client-facing form of the report, so the recipients are the project's
// Client and Architect contacts (the projects@ mailbox is cc'd automatically at the Graph-client
// chokepoint). The frozen report is rendered server-side and attached as a PDF — the same
// rendering the download endpoint streams, so what's downloaded and what's sent never diverge.
// The subject and HTML cover note are the caller's to edit before drafting.
public sealed record PrepareValuationReportSnapshotEmailDraft(
    string ValuationReportSnapshotId,
    string Subject,
    string HtmlBody) : ICommand<ValuationReportSnapshotEmailDraft>;

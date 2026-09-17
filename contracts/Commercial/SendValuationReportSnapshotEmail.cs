using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Commercial;

// Emails the frozen valuation report to the client from the shared projects mailbox. The snapshot
// is the only client-facing form of the report, so the recipients are the project's Client and
// Architect contacts (the projects@ mailbox is cc'd automatically at the Graph-client chokepoint).
// The report is rendered server-side and attached as a PDF — the same rendering the download
// endpoint streams, so what is downloaded and what is sent never diverge. The subject and HTML
// cover note are the caller's to edit first.
//
// SaveAsDraftOnly stops after staging, leaving the reviewed draft in the mailbox's Drafts folder
// for Outlook — what this command always did before 2026-09-17, now a choice rather than a fate.
// A failed send degrades to exactly that and says so, so the statement is never lost.
public sealed record SendValuationReportSnapshotEmail(
    string ValuationReportSnapshotId,
    string Subject,
    string HtmlBody,
    bool SaveAsDraftOnly = false) : ICommand<ValuationReportSnapshotEmailOutcome>;

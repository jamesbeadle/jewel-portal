using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Subcontractors;

// Emails the statement of account to the subcontractor from the shared projects mailbox. The
// recipient is their directory email; the statement is rendered server-side from the live register
// and attached as a PDF, so the figures always match the register at the moment it goes; the
// subject and HTML cover note are the caller's to edit first.
//
// SaveAsDraftOnly stops after staging, leaving the reviewed draft in the mailbox's Drafts folder
// for Outlook — what this command always did before 2026-09-17, now a choice rather than a fate.
// A failed send degrades to exactly that and says so, so the email is never lost.
public sealed record SendSubcontractorStatementEmail(
    string SubcontractorId,
    string Subject,
    string HtmlBody,
    bool SaveAsDraftOnly = false) : ICommand<SubcontractorStatementEmailOutcome>;

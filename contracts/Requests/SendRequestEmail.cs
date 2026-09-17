using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Requests;

/// <summary>
/// Emails the request's official document (the RFI PDF) from the projects mailbox as a new thread.
/// Recipients (To/CC/BCC) resolve through the shared correspondence profile — the request's linked
/// party first, then the project's party, then the project profile's To rows, with the profile
/// supplying the copied recipients. Pass <see cref="RecipientOverride"/> to address it to one
/// ad-hoc email instead (no CC/BCC).
///
/// SaveAsDraftOnly stops after staging, leaving the reviewed draft in the mailbox's Drafts folder
/// for Outlook — what this command always did before 2026-09-17, now a choice rather than a fate.
/// A failed send degrades to exactly that and says so, so the document is never lost.
/// </summary>
public sealed record SendRequestEmail(
    string RequestId,
    string? RecipientOverride = null,
    bool SaveAsDraftOnly = false) : ICommand<RequestEmailOutcome>;

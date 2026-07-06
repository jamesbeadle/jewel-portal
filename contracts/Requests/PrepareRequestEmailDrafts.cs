using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Requests;

/// <summary>
/// The bulk form of <see cref="PrepareRequestEmailDraft"/>: creates one Outlook draft in the
/// connected projects mailbox per request id — each carrying that request's official document PDF,
/// with To/CC/BCC resolved through the shared correspondence profile exactly as the single draft
/// does. Nothing is sent; every draft waits in the mailbox's Drafts folder for a person to review
/// and send.
///
/// Drafting continues past individual failures: each id gets its own
/// <see cref="RequestEmailDraftOutcome"/>, so one request with no resolvable recipient doesn't
/// block the rest. No ad-hoc recipient override exists here — overrides are a considered,
/// one-at-a-time act on the request detail page.
/// </summary>
public sealed record PrepareRequestEmailDrafts(
    IReadOnlyList<string> RequestIds) : ICommand<RequestEmailDraftBatch>;

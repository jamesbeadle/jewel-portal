using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Requests;

/// <summary>
/// The bulk form of <see cref="SendRequestEmail"/>: one email per request id, each carrying that
/// request's official document PDF, with To/CC/BCC resolved through the shared correspondence
/// profile exactly as the single one does. SaveAsDraftOnly applies to the whole run.
///
/// It continues past individual failures: each id gets its own <see cref="RequestEmailResult"/>,
/// so one request with no resolvable recipient doesn't block the rest. No ad-hoc recipient
/// override exists here — overrides are a considered, one-at-a-time act on the request detail
/// page.
/// </summary>
public sealed record SendRequestEmails(
    IReadOnlyList<string> RequestIds,
    bool SaveAsDraftOnly = false) : ICommand<RequestEmailBatch>;

using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Requests;

// Ask Claude to read one triage email (and its whole live thread, when ConversationId is present)
// and recommend the next triage action. Subject/FromEmail/FromName travel with the query so the
// no-thread fallback can still describe the email without a second Graph round-trip. Advisory
// read only — nothing is written, so this is a query despite the AI call behind it.
public sealed record RecommendTriageAction(
    string MessageId,
    string? InternetMessageId = null,
    string? ConversationId = null,
    string? Subject = null,
    string? FromEmail = null,
    string? FromName = null) : IQuery<TriageRecommendation>;

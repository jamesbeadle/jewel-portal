using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Requests;

/// <summary>
/// Preview exactly who a request's document would go to if issued right now — the same resolution
/// the send and draft paths use (request party → project party → project profile To rows for the
/// address line; the project's correspondence profile for Cc/Bcc). Pure read, nothing is sent.
/// </summary>
public sealed record ResolveRequestRecipients(string RequestId) : IQuery<RequestRecipientSet>;

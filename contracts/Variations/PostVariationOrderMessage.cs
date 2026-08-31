using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Variations;

/// <summary>POST /api/variation-orders/{voId}/messages — adds a message to the order's in-app
/// conversation. The author fields are stamped server-side from the signed-in session.</summary>
public sealed record PostVariationOrderMessage(
    string VariationOrderId,
    string Body,
    MessageVisibility Visibility,
    string AuthorEmail,
    string AuthorName,
    // The message this one replies to; null posts a top-level message. The handler rejects a
    // parent that doesn't exist on the same variation order.
    string? ParentMessageId = null) : ICommand<VariationOrderMessage>;

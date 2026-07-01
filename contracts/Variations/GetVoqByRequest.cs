using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Variations;

// Returns the VOQ created from a given request (RFQ), or null if none exists yet.
public sealed record GetVoqByRequest(string RequestId) : IQuery<VariationOrderQuote?>;

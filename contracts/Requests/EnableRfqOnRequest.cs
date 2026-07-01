using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Requests;

/// <summary>
/// Marks an RFI as carrying an RFQ. This unlocks creation of a Variation Order Quote (VOQ) from the
/// request. Only valid on a request that is already an RFI.
/// </summary>
public sealed record EnableRfqOnRequest(string RequestId) : ICommand<Request>;

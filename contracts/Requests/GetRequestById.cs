using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Requests;

public sealed record GetRequestById(string RequestId) : IQuery<Request?>;

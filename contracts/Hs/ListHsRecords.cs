using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Hs;

public sealed record ListHsRecords : IQuery<IReadOnlyList<HsRecord>>;

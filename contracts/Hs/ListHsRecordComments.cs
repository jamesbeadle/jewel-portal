using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Hs;

/// <summary>A record's thread, oldest first, each comment with its photographs.</summary>
public sealed record ListHsRecordComments(string HsRecordId) : IQuery<IReadOnlyList<HsRecordComment>>;

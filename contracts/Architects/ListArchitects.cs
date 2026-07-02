using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Architects;

public sealed record ListArchitects : IQuery<IReadOnlyList<Architect>>;

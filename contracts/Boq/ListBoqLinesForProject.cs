using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Boq;

public sealed record ListBoqLinesForProject(string ProjectId) : IQuery<IReadOnlyList<BoqLineItem>>;

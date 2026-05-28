using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Drawings;

public sealed record ListDrawingsForProject(string ProjectId) : IQuery<IReadOnlyList<Drawing>>;

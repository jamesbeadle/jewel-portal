using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Changes;

public sealed record ListChangesForProject(string ProjectId) : IQuery<IReadOnlyList<ChangeRecord>>;

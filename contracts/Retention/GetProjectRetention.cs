using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Retention;

// Null when the project has no retention terms recorded yet.
public sealed record GetProjectRetention(string ProjectId) : IQuery<ProjectRetention?>;

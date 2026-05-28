using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Closeout;

public sealed record GetRetentionForProject(string ProjectId) : IQuery<RetentionRelease?>;

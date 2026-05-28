using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Closeout;

public sealed record ReleaseRetention(
    string ProjectId,
    decimal Amount,
    bool IsPublishedDownstream) : ICommand<RetentionRelease>;

using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Contracts.Platform;

namespace Jewel.JPMS.Api.Features.Platform.Commands;

public sealed class PublishAppVersionValidation
{
    // Nothing to check: the command carries no client-supplied fields — the increment is decided
    // by the handler and PublishedBy is stamped from the resolved caller.
    public ValidationOutcome Check(PublishAppVersion command) => ValidationOutcome.Passed;
}

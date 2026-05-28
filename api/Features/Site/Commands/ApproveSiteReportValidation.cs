using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Contracts.Site;

namespace Jewel.JPMS.Api.Features.Site.Commands;

public sealed class ApproveSiteReportValidation
{
    public ValidationOutcome Check(ApproveSiteReport command)
    {
        if (string.IsNullOrWhiteSpace(command.SiteReportId)) return ValidationOutcome.Failed("SiteReportId is required.");
        return ValidationOutcome.Passed;
    }
}

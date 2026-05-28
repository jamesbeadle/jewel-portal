using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Site;

public sealed record ApproveSiteReport(string SiteReportId) : ICommand<SiteReport>;

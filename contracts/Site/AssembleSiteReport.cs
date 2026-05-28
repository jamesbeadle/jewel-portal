using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Site;

public sealed record AssembleSiteReport(
    string ProjectId,
    DateTimeOffset PeriodEnd,
    string Narrative,
    int AttendanceDays,
    int OpenSnags,
    decimal ProgressPercent) : ICommand<SiteReport>;

using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Commercial;

public sealed record SubmitTimesheet(
    string ProjectId,
    string PersonEmail,
    DateTimeOffset WorkedOn,
    decimal Hours,
    string CostCode) : ICommand<Timesheet>;

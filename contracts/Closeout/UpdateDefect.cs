using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Closeout;

public sealed record UpdateDefect(
    string DefectId,
    string Description,
    string Location,
    string AssignedToEmail,
    DefectStatus Status) : ICommand<Defect>;

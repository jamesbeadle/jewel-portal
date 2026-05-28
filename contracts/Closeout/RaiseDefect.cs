using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Closeout;

public sealed record RaiseDefect(
    string ProjectId,
    string Description,
    string Location,
    string AssignedToEmail) : ICommand<Defect>;

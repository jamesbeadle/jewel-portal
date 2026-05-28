using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Boq;

public sealed record SignOffBoqForProject(
    string ProjectId,
    string SignedOffByEmail,
    decimal TenderTotalAtSignOff) : ICommand<BoqSignOff>;

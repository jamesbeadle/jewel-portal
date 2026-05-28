using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Boq;

public sealed record GetBoqSignOffForProject(string ProjectId) : IQuery<BoqSignOff?>;

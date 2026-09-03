using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.SiteInstructions;

public sealed record ListSiteInstructionsForProject(string ProjectId) : IQuery<IReadOnlyList<SiteInstruction>>;

using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.CommercialInputs;

public sealed record ListSubcontractorRetentionsForProject(string ProjectId)
    : IQuery<IReadOnlyList<SubcontractorRetention>>;

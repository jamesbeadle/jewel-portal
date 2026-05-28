using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.CommercialInputs;

public sealed record RecordSubcontractorRetention(
    string ProjectId,
    string SubcontractorReference,
    decimal CertifiedAmount,
    decimal RetentionPercent,
    decimal FirstReleasedAmount,
    decimal FinalReleasedAmount) : ICommand<SubcontractorRetention>;

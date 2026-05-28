using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.CommercialInputs;

public sealed record RecordContraCharge(
    string ProjectId,
    string SubcontractorReference,
    DateTimeOffset RaisedOn,
    string Description,
    string Category,
    decimal Amount,
    string Status,
    decimal RecoveredAmount) : ICommand<ContraCharge>;

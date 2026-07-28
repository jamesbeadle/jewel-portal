using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Subcontractors;

public sealed record UpdateSubcontractor(
    string SubcontractorId,
    string CompanyName,
    IReadOnlyList<string> TradeIds,
    string ContactName,
    string ContactEmail,
    string ContactPhone,
    string CisStatus,
    // Payment terms printed on the company's purchase orders ("30 day terms"). Null means
    // "leave unchanged", so callers that only touch other fields never reset an override.
    int? PaymentTermsDays = null) : ICommand<Subcontractor>;

using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Subcontractors;

public sealed record UpdateSubcontractor(
    string SubcontractorId,
    string CompanyName,
    string PrimaryTrade,
    string ContactName,
    string ContactEmail,
    string ContactPhone,
    string CisStatus) : ICommand<Subcontractor>;

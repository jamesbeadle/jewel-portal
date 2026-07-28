using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Subcontractors;

public sealed record AddSubcontractorToDirectory(
    string CompanyName,
    IReadOnlyList<string> TradeIds,
    string ContactName,
    string ContactEmail,
    string ContactPhone,
    string CisStatus,
    DirectoryCategory Category = DirectoryCategory.Subcontractor,
    string MobileNumber = "",
    string Town = "",
    string County = "",
    string Website = "",
    // Payment terms printed on the company's purchase orders ("30 day terms"); 30 by default.
    int PaymentTermsDays = 30) : ICommand<Subcontractor>;

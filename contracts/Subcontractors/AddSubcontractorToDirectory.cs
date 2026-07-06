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
    string Website = "") : ICommand<Subcontractor>;

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
    int PaymentTermsDays = 30,
    // Street line(s) and postcode of the company's postal address (Town/County above complete
    // the letter block printed at the top of its purchase orders).
    string AddressLine = "",
    string Postcode = "",
    // True when the record is being minted only so a bid-package tender list can hold the company
    // (quick-add / local search). The record exists but stays out of the Directory until promoted
    // via PromoteSubcontractorToDirectory or an award. The Directory's own add-company form never
    // sets this.
    bool IsProspect = false) : ICommand<Subcontractor>;

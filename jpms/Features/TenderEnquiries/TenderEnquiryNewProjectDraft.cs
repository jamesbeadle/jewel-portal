using Jewel.JPMS.Contracts.TenderEnquiries;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Features.TenderEnquiries;

/// <summary>The Lead-stage project an enquiry creates when the job is new to Jewel, as typed —
/// shared by the register's Log enquiry dialog and the Control Centre's staged create.</summary>
public sealed class TenderEnquiryNewProjectDraft
{
    public string Name { get; set; } = "";
    public string ClientName { get; set; } = "";
    public Organisation Organisation { get; set; } = Organisation.JewelBespokeBuild;
    public string AddressLine { get; set; } = "";
    public string Town { get; set; } = "";
    public string Postcode { get; set; } = "";

    public string? Problem => string.IsNullOrWhiteSpace(Name) ? "Name the new project." : null;

    public TenderEnquiryProjectDraft ToDraft() =>
        new(Name.Trim(), ClientName.Trim(), Organisation, AddressLine.Trim(), Town.Trim(), Postcode.Trim());
}

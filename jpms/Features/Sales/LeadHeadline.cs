using Jewel.JPMS.Models;

namespace Jewel.JPMS.Features.Sales;

public static class LeadHeadline
{
    public static string Headline(this Lead lead) =>
        string.IsNullOrWhiteSpace(lead.ContactName) ? lead.CompanyName : lead.ContactName;
}

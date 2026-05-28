using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Leads;

public sealed record ListSiteVisitsForLead(string LeadId) : IQuery<IReadOnlyList<SiteVisit>>;

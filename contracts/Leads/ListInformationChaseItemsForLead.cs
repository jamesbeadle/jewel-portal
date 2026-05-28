using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Leads;

public sealed record ListInformationChaseItemsForLead(string LeadId) : IQuery<IReadOnlyList<InfoChaseItem>>;

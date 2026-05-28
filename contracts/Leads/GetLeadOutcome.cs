using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Leads;

public sealed record GetLeadOutcome(string LeadId) : IQuery<LeadOutcome?>;

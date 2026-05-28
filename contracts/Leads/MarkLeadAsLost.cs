using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Leads;

public sealed record MarkLeadAsLost(
    string LeadId,
    string Reason,
    string DecidedByEmail) : ICommand<LeadOutcome>;

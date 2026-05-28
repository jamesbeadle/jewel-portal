using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Leads;

public sealed record MarkLeadAsWon(
    string LeadId,
    string DecidedByEmail) : ICommand<LeadOutcome>;

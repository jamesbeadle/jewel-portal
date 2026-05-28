using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Leads;

public sealed record ReviseProposal(
    string LeadId,
    decimal RevisedValue,
    string Notes) : ICommand<Proposal>;

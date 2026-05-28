using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Leads;

public sealed record IssueProposal(
    string LeadId,
    decimal Value) : ICommand<Proposal>;

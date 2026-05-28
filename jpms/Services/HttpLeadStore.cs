using Jewel.JPMS.Contracts.Leads;
using Jewel.JPMS.Cqrs;
using Jewel.JPMS.Features.Leads;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Services;

public sealed class HttpLeadStore : ILeadStore
{
    private readonly LeadPipelineReadModel readModel;
    private readonly IQueryClient queries;
    private readonly ICommandSender commands;

    public HttpLeadStore(LeadPipelineReadModel readModel, IQueryClient queries, ICommandSender commands)
    {
        this.readModel = readModel;
        this.queries = queries;
        this.commands = commands;
        readModel.OnChanged += () => OnChange?.Invoke();
    }

    public event Action? OnChange;

    public IReadOnlyList<Lead> All()
    {
        if (readModel.Current is null) _ = readModel.RefreshAsync(CancellationToken.None);
        return readModel.Current ?? Array.Empty<Lead>();
    }

    public Lead? Find(string leadId) =>
        All().FirstOrDefault(lead => string.Equals(lead.LeadId, leadId, StringComparison.OrdinalIgnoreCase));

    public Lead Upsert(Lead lead)
    {
        if (Find(lead.LeadId) is null) _ = CaptureLeadAsync(lead);
        else _ = UpdateLeadAsync(lead);
        return lead;
    }

    public QualificationAssessment? GetQualification(string leadId) =>
        queries.AskAsync(new GetLeadQualification(leadId), CancellationToken.None).GetAwaiter().GetResult();

    public void SaveQualification(QualificationAssessment assessment) =>
        _ = commands.SendAsync(
            new RecordLeadQualificationScore(assessment.LeadId, assessment.Score, assessment.Notes, assessment.AssessedByEmail),
            CancellationToken.None);

    public IReadOnlyList<SiteVisit> SiteVisitsFor(string leadId) =>
        queries.AskAsync(new ListSiteVisitsForLead(leadId), CancellationToken.None).GetAwaiter().GetResult();

    public void SaveSiteVisit(SiteVisit visit)
    {
        if (string.IsNullOrEmpty(visit.SiteVisitId))
            _ = commands.SendAsync(new BookSiteVisit(visit.LeadId, visit.ScheduledAt, visit.AttendeeEmails), CancellationToken.None);
        else
            _ = commands.SendAsync(new RecordSiteVisitNotes(visit.SiteVisitId, visit.Notes, visit.PhotoCount, visit.IsComplete), CancellationToken.None);
    }

    public IReadOnlyList<InfoChaseItem> InfoChaseFor(string leadId) =>
        queries.AskAsync(new ListInformationChaseItemsForLead(leadId), CancellationToken.None).GetAwaiter().GetResult();

    public void SaveInfoChaseItem(InfoChaseItem item) =>
        _ = commands.SendAsync(
            new RecordInformationChaseItem(item.LeadId, item.Kind, item.Description, item.IsReceived),
            CancellationToken.None);

    public BidDecision? GetBidDecision(string leadId) =>
        queries.AskAsync(new GetBidDecisionForLead(leadId), CancellationToken.None).GetAwaiter().GetResult();

    public void SaveBidDecision(BidDecision decision) =>
        _ = commands.SendAsync(
            new RecordBidDecision(decision.LeadId, decision.ShouldBid, decision.Reason, decision.DecidedByEmail),
            CancellationToken.None);

    public Proposal? GetProposal(string leadId) =>
        queries.AskAsync(new GetProposalForLead(leadId), CancellationToken.None).GetAwaiter().GetResult();

    public void SaveProposal(Proposal proposal)
    {
        if (string.IsNullOrEmpty(proposal.ProposalId))
            _ = commands.SendAsync(new IssueProposal(proposal.LeadId, proposal.Value), CancellationToken.None);
        else
            _ = commands.SendAsync(new ReviseProposal(proposal.LeadId, proposal.Value, "Revised via legacy shim"), CancellationToken.None);
    }

    public LeadOutcome? GetOutcome(string leadId) =>
        queries.AskAsync(new GetLeadOutcome(leadId), CancellationToken.None).GetAwaiter().GetResult();

    public void SaveOutcome(LeadOutcome outcome)
    {
        if (outcome.IsWon) _ = commands.SendAsync(new MarkLeadAsWon(outcome.LeadId, outcome.DecidedByEmail), CancellationToken.None);
        else _ = commands.SendAsync(new MarkLeadAsLost(outcome.LeadId, outcome.Reason, outcome.DecidedByEmail), CancellationToken.None);
    }

    private async Task CaptureLeadAsync(Lead lead)
    {
        await commands.SendAsync(
            new CaptureLead(lead.Reference, lead.ContactName, lead.ContactEmail, lead.ContactPhone, lead.CompanyName, lead.SiteAddress, lead.EstimatedValue, lead.Source, lead.OwnerEmail),
            CancellationToken.None);
        await readModel.RefreshAsync(CancellationToken.None);
    }

    private async Task UpdateLeadAsync(Lead lead)
    {
        await commands.SendAsync(
            new UpdateLeadDetails(lead.LeadId, lead.Reference, lead.ContactName, lead.ContactEmail, lead.ContactPhone, lead.CompanyName, lead.SiteAddress, lead.EstimatedValue, lead.Source, lead.Stage, lead.OwnerEmail),
            CancellationToken.None);
        await readModel.RefreshAsync(CancellationToken.None);
    }
}

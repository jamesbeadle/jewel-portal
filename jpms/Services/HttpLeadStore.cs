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

    // Per-lead lists read during render, cached so they never block on async (which deadlocks
    // on WebAssembly). Saving invalidates the affected lead. The single-value getters below are
    // async instead, because their callers assign the result to a field in a lifecycle method.
    private readonly AsyncQueryCache<string, IReadOnlyList<SiteVisit>> siteVisits;
    private readonly AsyncQueryCache<string, IReadOnlyList<InfoChaseItem>> infoChase;

    public HttpLeadStore(LeadPipelineReadModel readModel, IQueryClient queries, ICommandSender commands)
    {
        this.readModel = readModel;
        this.queries = queries;
        this.commands = commands;
        readModel.OnChanged += () => OnChange?.Invoke();
        siteVisits = new((id, ct) => queries.AskAsync(new ListSiteVisitsForLead(id), ct), () => OnChange?.Invoke());
        infoChase = new((id, ct) => queries.AskAsync(new ListInformationChaseItemsForLead(id), ct), () => OnChange?.Invoke());
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

    public Task<QualificationAssessment?> GetQualificationAsync(string leadId) =>
        queries.AskAsync(new GetLeadQualification(leadId), CancellationToken.None);

    public void SaveQualification(QualificationAssessment assessment) =>
        _ = commands.SendAsync(
            new RecordLeadQualificationScore(assessment.LeadId, assessment.Score, assessment.Notes, assessment.AssessedByEmail),
            CancellationToken.None);

    public IReadOnlyList<SiteVisit> SiteVisitsFor(string leadId) =>
        siteVisits.Get(leadId, Array.Empty<SiteVisit>());

    public void SaveSiteVisit(SiteVisit visit) => _ = SaveSiteVisitAsync(visit);

    private async Task SaveSiteVisitAsync(SiteVisit visit)
    {
        if (string.IsNullOrEmpty(visit.SiteVisitId))
            await commands.SendAsync(new BookSiteVisit(visit.LeadId, visit.ScheduledAt, visit.AttendeeEmails), CancellationToken.None);
        else
            await commands.SendAsync(new RecordSiteVisitNotes(visit.SiteVisitId, visit.Notes, visit.PhotoCount, visit.IsComplete), CancellationToken.None);
        siteVisits.Invalidate(visit.LeadId);
    }

    public IReadOnlyList<InfoChaseItem> InfoChaseFor(string leadId) =>
        infoChase.Get(leadId, Array.Empty<InfoChaseItem>());

    public void SaveInfoChaseItem(InfoChaseItem item) => _ = SaveInfoChaseItemAsync(item);

    private async Task SaveInfoChaseItemAsync(InfoChaseItem item)
    {
        await commands.SendAsync(
            new RecordInformationChaseItem(item.LeadId, item.Kind, item.Description, item.IsReceived),
            CancellationToken.None);
        infoChase.Invalidate(item.LeadId);
    }

    public Task<BidDecision?> GetBidDecisionAsync(string leadId) =>
        queries.AskAsync(new GetBidDecisionForLead(leadId), CancellationToken.None);

    public void SaveBidDecision(BidDecision decision) =>
        _ = commands.SendAsync(
            new RecordBidDecision(decision.LeadId, decision.ShouldBid, decision.Reason, decision.DecidedByEmail),
            CancellationToken.None);

    public Task<Proposal?> GetProposalAsync(string leadId) =>
        queries.AskAsync(new GetProposalForLead(leadId), CancellationToken.None);

    public void SaveProposal(Proposal proposal)
    {
        if (string.IsNullOrEmpty(proposal.ProposalId))
            _ = commands.SendAsync(new IssueProposal(proposal.LeadId, proposal.Value), CancellationToken.None);
        else
            _ = commands.SendAsync(new ReviseProposal(proposal.LeadId, proposal.Value, "Revised via legacy shim"), CancellationToken.None);
    }

    public Task<LeadOutcome?> GetOutcomeAsync(string leadId) =>
        queries.AskAsync(new GetLeadOutcome(leadId), CancellationToken.None);

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

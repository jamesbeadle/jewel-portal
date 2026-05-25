using Jewel.JPMS.Models;

namespace Jewel.JPMS.Services;

public sealed class InMemoryLeadStore : ILeadStore
{
    private readonly List<Lead> leads = LeadSeed.Leads();
    private readonly Dictionary<string, QualificationAssessment> qualifications = new();
    private readonly Dictionary<string, List<SiteVisit>> siteVisits = new();
    private readonly Dictionary<string, List<InfoChaseItem>> infoChaseItems = LeadSeed.InfoChase();
    private readonly Dictionary<string, BidDecision> bidDecisions = new();
    private readonly Dictionary<string, Proposal> proposals = new();
    private readonly Dictionary<string, LeadOutcome> outcomes = new();

    public event Action? OnChange;

    public IReadOnlyList<Lead> All() => leads.AsReadOnly();

    public Lead? Find(string leadId) =>
        leads.FirstOrDefault(lead =>
            string.Equals(lead.LeadId, leadId, StringComparison.OrdinalIgnoreCase));

    public Lead Upsert(Lead lead)
    {
        var existing = Find(lead.LeadId);
        if (existing is not null) leads.Remove(existing);
        leads.Add(lead);
        OnChange?.Invoke();
        return lead;
    }

    public QualificationAssessment? GetQualification(string leadId) =>
        qualifications.TryGetValue(leadId, out var assessment) ? assessment : null;

    public void SaveQualification(QualificationAssessment assessment)
    {
        qualifications[assessment.LeadId] = assessment;
        OnChange?.Invoke();
    }

    public IReadOnlyList<SiteVisit> SiteVisitsFor(string leadId) =>
        siteVisits.TryGetValue(leadId, out var visits) ? visits.AsReadOnly() : Array.Empty<SiteVisit>();

    public void SaveSiteVisit(SiteVisit visit)
    {
        if (!siteVisits.TryGetValue(visit.LeadId, out var list))
        {
            list = new List<SiteVisit>();
            siteVisits[visit.LeadId] = list;
        }
        list.RemoveAll(existing => existing.SiteVisitId == visit.SiteVisitId);
        list.Add(visit);
        OnChange?.Invoke();
    }

    public IReadOnlyList<InfoChaseItem> InfoChaseFor(string leadId) =>
        infoChaseItems.TryGetValue(leadId, out var items) ? items.AsReadOnly() : Array.Empty<InfoChaseItem>();

    public void SaveInfoChaseItem(InfoChaseItem item)
    {
        if (!infoChaseItems.TryGetValue(item.LeadId, out var list))
        {
            list = new List<InfoChaseItem>();
            infoChaseItems[item.LeadId] = list;
        }
        list.RemoveAll(existing => existing.InfoChaseItemId == item.InfoChaseItemId);
        list.Add(item);
        OnChange?.Invoke();
    }

    public BidDecision? GetBidDecision(string leadId) =>
        bidDecisions.TryGetValue(leadId, out var decision) ? decision : null;

    public void SaveBidDecision(BidDecision decision)
    {
        bidDecisions[decision.LeadId] = decision;
        OnChange?.Invoke();
    }

    public Proposal? GetProposal(string leadId) =>
        proposals.TryGetValue(leadId, out var proposal) ? proposal : null;

    public void SaveProposal(Proposal proposal)
    {
        proposals[proposal.LeadId] = proposal;
        OnChange?.Invoke();
    }

    public LeadOutcome? GetOutcome(string leadId) =>
        outcomes.TryGetValue(leadId, out var outcome) ? outcome : null;

    public void SaveOutcome(LeadOutcome outcome)
    {
        outcomes[outcome.LeadId] = outcome;
        OnChange?.Invoke();
    }
}

using Jewel.JPMS.Models;

namespace Jewel.JPMS.Services;

public interface ILeadStore
{
    IReadOnlyList<Lead> All();

    Lead? Find(string leadId);

    Lead Upsert(Lead lead);

    Task<QualificationAssessment?> GetQualificationAsync(string leadId);
    void SaveQualification(QualificationAssessment assessment);

    IReadOnlyList<SiteVisit> SiteVisitsFor(string leadId);
    void SaveSiteVisit(SiteVisit visit);

    IReadOnlyList<InfoChaseItem> InfoChaseFor(string leadId);
    void SaveInfoChaseItem(InfoChaseItem item);

    Task<BidDecision?> GetBidDecisionAsync(string leadId);
    void SaveBidDecision(BidDecision decision);

    Task<Proposal?> GetProposalAsync(string leadId);
    void SaveProposal(Proposal proposal);

    Task<LeadOutcome?> GetOutcomeAsync(string leadId);
    void SaveOutcome(LeadOutcome outcome);

    event Action? OnChange;
}

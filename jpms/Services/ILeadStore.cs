using Jewel.JPMS.Models;

namespace Jewel.JPMS.Services;

public interface ILeadStore
{
    IReadOnlyList<Lead> All();

    Lead? Find(string leadId);

    Lead Upsert(Lead lead);

    QualificationAssessment? GetQualification(string leadId);
    void SaveQualification(QualificationAssessment assessment);

    IReadOnlyList<SiteVisit> SiteVisitsFor(string leadId);
    void SaveSiteVisit(SiteVisit visit);

    IReadOnlyList<InfoChaseItem> InfoChaseFor(string leadId);
    void SaveInfoChaseItem(InfoChaseItem item);

    BidDecision? GetBidDecision(string leadId);
    void SaveBidDecision(BidDecision decision);

    Proposal? GetProposal(string leadId);
    void SaveProposal(Proposal proposal);

    LeadOutcome? GetOutcome(string leadId);
    void SaveOutcome(LeadOutcome outcome);

    event Action? OnChange;
}

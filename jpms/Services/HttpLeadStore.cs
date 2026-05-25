using System.Net.Http.Json;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Services;

public sealed class HttpLeadStore : ILeadStore
{
    private readonly HttpClient httpClient;
    private IReadOnlyList<Lead> cachedLeads = Array.Empty<Lead>();
    private bool hasLoaded;

    public HttpLeadStore(HttpClient httpClient) { this.httpClient = httpClient; }

    public event Action? OnChange;

    public IReadOnlyList<Lead> All()
    {
        if (!hasLoaded) _ = LoadLeadsAsync();
        return cachedLeads;
    }

    public Lead? Find(string leadId) =>
        cachedLeads.FirstOrDefault(lead =>
            string.Equals(lead.LeadId, leadId, StringComparison.OrdinalIgnoreCase));

    public Lead Upsert(Lead lead)
    {
        _ = PostAsync("/api/leads", lead, refresh: true);
        return lead;
    }

    public QualificationAssessment? GetQualification(string leadId) =>
        FetchSync<QualificationAssessment>($"/api/leads/{leadId}/qualification");

    public void SaveQualification(QualificationAssessment assessment) =>
        _ = PostAsync("/api/leads/qualifications", assessment, refresh: false);

    public IReadOnlyList<SiteVisit> SiteVisitsFor(string leadId) =>
        FetchSync<List<SiteVisit>>($"/api/leads/{leadId}/site-visits")?.AsReadOnly() ?? (IReadOnlyList<SiteVisit>)Array.Empty<SiteVisit>();

    public void SaveSiteVisit(SiteVisit visit) =>
        _ = PostAsync("/api/leads/site-visits", visit, refresh: false);

    public IReadOnlyList<InfoChaseItem> InfoChaseFor(string leadId) =>
        FetchSync<List<InfoChaseItem>>($"/api/leads/{leadId}/info-chase")?.AsReadOnly() ?? (IReadOnlyList<InfoChaseItem>)Array.Empty<InfoChaseItem>();

    public void SaveInfoChaseItem(InfoChaseItem item) =>
        _ = PostAsync("/api/leads/info-chase", item, refresh: false);

    public BidDecision? GetBidDecision(string leadId) =>
        FetchSync<BidDecision>($"/api/leads/{leadId}/bid-decision");

    public void SaveBidDecision(BidDecision decision) =>
        _ = PostAsync("/api/leads/bid-decisions", decision, refresh: false);

    public Proposal? GetProposal(string leadId) =>
        FetchSync<Proposal>($"/api/leads/{leadId}/proposal");

    public void SaveProposal(Proposal proposal) =>
        _ = PostAsync("/api/leads/proposals", proposal, refresh: false);

    public LeadOutcome? GetOutcome(string leadId) =>
        FetchSync<LeadOutcome>($"/api/leads/{leadId}/outcome");

    public void SaveOutcome(LeadOutcome outcome) =>
        _ = PostAsync("/api/leads/outcomes", outcome, refresh: false);

    private async Task LoadLeadsAsync()
    {
        hasLoaded = true;
        try
        {
            var response = await httpClient.GetFromJsonAsync<List<Lead>>("/api/leads");
            cachedLeads = response?.AsReadOnly() ?? (IReadOnlyList<Lead>)Array.Empty<Lead>();
            OnChange?.Invoke();
        }
        catch { cachedLeads = Array.Empty<Lead>(); }
    }

    private async Task PostAsync<T>(string url, T body, bool refresh)
    {
        try { await httpClient.PostAsJsonAsync(url, body); }
        catch { return; }
        if (refresh) await LoadLeadsAsync();
        else OnChange?.Invoke();
    }

    private T? FetchSync<T>(string url)
    {
        try { return httpClient.GetFromJsonAsync<T>(url).GetAwaiter().GetResult(); }
        catch { return default; }
    }
}

using Jewel.JPMS.Models;

namespace Jewel.JPMS.Services;

internal static class LeadSeed
{
    private const string NigelEmail = "Nigel.Reilly@jewelenterprises.co.uk";

    public static List<Lead> Leads() => new()
    {
        new Lead("LD-001", "LD-2026-001",
            "Adrian Murray", "adrian.murray@example.com", "+44 7700 900123",
            "Murray Family Office", "Esher, Surrey",
            2_400_000m, LeadSource.Architect, LeadStage.Tendering,
            NigelEmail, DateTimeOffset.UtcNow.AddDays(-14)),
        new Lead("LD-002", "LD-2026-002",
            "Sarah Whitfield", "swhitfield@example.com", "+44 7700 900456",
            "", "Cobham, Surrey",
            1_650_000m, LeadSource.Website, LeadStage.SurveyBooked,
            NigelEmail, DateTimeOffset.UtcNow.AddDays(-5)),
        new Lead("LD-003", "LD-2026-003",
            "James Harrington", "jharrington@example.com", "+44 7700 900789",
            "Harrington Holdings", "Weybridge, Surrey",
            3_100_000m, LeadSource.Referral, LeadStage.ProposalIssued,
            NigelEmail, DateTimeOffset.UtcNow.AddDays(-28)),
        new Lead("LD-004", "LD-2026-004",
            "Priya Sharma", "psharma@example.com", "+44 7700 900012",
            "", "Oxshott, Surrey",
            980_000m, LeadSource.Instagram, LeadStage.Lost,
            NigelEmail, DateTimeOffset.UtcNow.AddDays(-60))
    };

    public static Dictionary<string, List<InfoChaseItem>> InfoChase() => new()
    {
        ["LD-002"] = new List<InfoChaseItem>
        {
            new("IC-001", "LD-002", "Drawing", "Architectural plans (PDF)", false, DateTimeOffset.UtcNow.AddDays(-3)),
            new("IC-002", "LD-002", "Consent", "Planning consent reference", false, DateTimeOffset.UtcNow.AddDays(-3))
        }
    };
}

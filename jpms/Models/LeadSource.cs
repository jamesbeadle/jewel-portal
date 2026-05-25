namespace Jewel.JPMS.Models;

public enum LeadSource
{
    Website,
    Instagram,
    LinkedIn,
    Referral,
    Architect,
    RepeatClient,
    Manual
}

public static class LeadSourceExtensions
{
    public static string DisplayName(this LeadSource source) => source switch
    {
        LeadSource.Website      => "Website",
        LeadSource.Instagram    => "Instagram",
        LeadSource.LinkedIn     => "LinkedIn",
        LeadSource.Referral     => "Referral",
        LeadSource.Architect    => "Architect Introduction",
        LeadSource.RepeatClient => "Repeat Client",
        LeadSource.Manual       => "Manual Entry",
        _ => source.ToString()
    };
}

namespace Jewel.JPMS.Models;

public enum LeadStage
{
    NewLead,
    Qualified,
    SurveyBooked,
    SurveyComplete,
    AwaitingInformation,
    DrawingsReceived,
    FeasibilityReview,
    Tendering,
    ProposalIssued,
    Negotiation,
    Won,
    Lost,
    Nurture
}

public static class LeadStageExtensions
{
    public static string DisplayName(this LeadStage stage) => stage switch
    {
        LeadStage.NewLead             => "New Lead",
        LeadStage.Qualified           => "Qualified",
        LeadStage.SurveyBooked        => "Survey Booked",
        LeadStage.SurveyComplete      => "Survey Complete",
        LeadStage.AwaitingInformation => "Awaiting Information",
        LeadStage.DrawingsReceived    => "Drawings Received",
        LeadStage.FeasibilityReview   => "Feasibility Review",
        LeadStage.Tendering           => "Tendering",
        LeadStage.ProposalIssued      => "Proposal Issued",
        LeadStage.Negotiation         => "Negotiation",
        LeadStage.Won                 => "Won",
        LeadStage.Lost                => "Lost",
        LeadStage.Nurture             => "Nurture",
        _ => stage.ToString()
    };

    public static string AccentDotClass(this LeadStage stage) => stage switch
    {
        LeadStage.NewLead             => "bg-slate-400",
        LeadStage.Qualified           => "bg-sky-500",
        LeadStage.SurveyBooked        => "bg-indigo-500",
        LeadStage.SurveyComplete      => "bg-indigo-600",
        LeadStage.AwaitingInformation => "bg-amber-500",
        LeadStage.DrawingsReceived    => "bg-amber-600",
        LeadStage.FeasibilityReview   => "bg-violet-500",
        LeadStage.Tendering           => "bg-violet-600",
        LeadStage.ProposalIssued      => "bg-emerald-500",
        LeadStage.Negotiation         => "bg-emerald-600",
        LeadStage.Won                 => "bg-slate-900",
        LeadStage.Lost                => "bg-rose-500",
        LeadStage.Nurture             => "bg-slate-500",
        _ => "bg-slate-400"
    };

    public static bool IsActive(this LeadStage stage) =>
        stage is not (LeadStage.Won or LeadStage.Lost or LeadStage.Nurture);
}

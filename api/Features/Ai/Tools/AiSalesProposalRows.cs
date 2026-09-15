namespace Jewel.JPMS.Api.Features.Ai.Tools;

// The proposal as the actions take it (2026-09-15): proposalId is what send_sales_proposal and
// withdraw_sales_proposal want, and a Draft's id is what save_sales_proposal rewrites.
internal static class AiSalesProposalRows
{
    public static object ProposalRow(SalesProposal proposal) => new
    {
        proposal.ProposalId,
        proposal.Version,
        proposal.Title,
        proposal.Scope,
        proposal.BasePrice,
        options = proposal.Options.Select(option => new
        {
            option.OptionId, option.Name, option.Description, option.PriceDelta, option.Recommended
        }),
        schedule = proposal.Schedule.Select(phase => new { phase.Name, phase.StartWeek, phase.Weeks }),
        proposal.Terms,
        proposal.HeroImageId,
        status = proposal.Status.ToString(),
        proposal.CreatedByEmail,
        proposal.CreatedAt,
        proposal.UpdatedAt,
        proposal.SentAt,
        proposal.AcceptedAt,
        proposal.AcceptedByName,
        proposal.AcceptedPrice,
        proposal.DeclinedAt,
        proposal.DeclineReason
    };
}

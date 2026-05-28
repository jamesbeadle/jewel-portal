using Jewel.JPMS.Contracts.Leads;
using Jewel.JPMS.Cqrs;
using Jewel.JPMS.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Jewel.JPMS.Features.Leads;

public static class LeadsRouteRegistration
{
    public static IServiceCollection AddLeadsReadModels(this IServiceCollection services)
    {
        services.AddScoped<LeadPipelineReadModel>();
        return services;
    }

    public static void RegisterLeadsRoutes(QueryRouteTable queries, CommandRouteTable commands)
    {
        queries.Register<ListLeadsInPipeline, IReadOnlyList<Lead>>(QueryRoute.Static("/api/leads"));

        queries.Register<GetLeadQualification, QualificationAssessment?>(
            new QueryRoute("/api/leads/{leadId}/qualification",
                query => $"/api/leads/{((GetLeadQualification)query).LeadId}/qualification"));

        queries.Register<GetBidDecisionForLead, BidDecision?>(
            new QueryRoute("/api/leads/{leadId}/bid-decision",
                query => $"/api/leads/{((GetBidDecisionForLead)query).LeadId}/bid-decision"));

        queries.Register<ListSiteVisitsForLead, IReadOnlyList<SiteVisit>>(
            new QueryRoute("/api/leads/{leadId}/site-visits",
                query => $"/api/leads/{((ListSiteVisitsForLead)query).LeadId}/site-visits"));

        queries.Register<ListInformationChaseItemsForLead, IReadOnlyList<InfoChaseItem>>(
            new QueryRoute("/api/leads/{leadId}/info-chase",
                query => $"/api/leads/{((ListInformationChaseItemsForLead)query).LeadId}/info-chase"));

        queries.Register<GetProposalForLead, Proposal?>(
            new QueryRoute("/api/leads/{leadId}/proposal",
                query => $"/api/leads/{((GetProposalForLead)query).LeadId}/proposal"));

        queries.Register<GetLeadOutcome, LeadOutcome?>(
            new QueryRoute("/api/leads/{leadId}/outcome",
                query => $"/api/leads/{((GetLeadOutcome)query).LeadId}/outcome"));

        commands.Register<CaptureLead, Lead>(CommandRoute.Post("/api/leads"));
        commands.Register<UpdateLeadDetails, Lead>(new CommandRoute("PUT", "/api/leads/{leadId}",
            command => $"/api/leads/{((UpdateLeadDetails)command).LeadId}"));
        commands.Register<RecordLeadQualificationScore, QualificationAssessment>(new CommandRoute("POST", "/api/leads/{leadId}/qualification",
            command => $"/api/leads/{((RecordLeadQualificationScore)command).LeadId}/qualification"));
        commands.Register<BookSiteVisit, SiteVisit>(new CommandRoute("POST", "/api/leads/{leadId}/site-visits",
            command => $"/api/leads/{((BookSiteVisit)command).LeadId}/site-visits"));
        commands.Register<RecordSiteVisitNotes, SiteVisit>(new CommandRoute("PUT", "/api/site-visits/{siteVisitId}",
            command => $"/api/site-visits/{((RecordSiteVisitNotes)command).SiteVisitId}"));
        commands.Register<RecordInformationChaseItem, InfoChaseItem>(new CommandRoute("POST", "/api/leads/{leadId}/info-chase",
            command => $"/api/leads/{((RecordInformationChaseItem)command).LeadId}/info-chase"));
        commands.Register<RecordBidDecision, BidDecision>(new CommandRoute("POST", "/api/leads/{leadId}/bid-decision",
            command => $"/api/leads/{((RecordBidDecision)command).LeadId}/bid-decision"));
        commands.Register<IssueProposal, Proposal>(new CommandRoute("POST", "/api/leads/{leadId}/proposal",
            command => $"/api/leads/{((IssueProposal)command).LeadId}/proposal"));
        commands.Register<ReviseProposal, Proposal>(new CommandRoute("PUT", "/api/leads/{leadId}/proposal",
            command => $"/api/leads/{((ReviseProposal)command).LeadId}/proposal"));
        commands.Register<MarkLeadAsWon, LeadOutcome>(new CommandRoute("POST", "/api/leads/{leadId}/won",
            command => $"/api/leads/{((MarkLeadAsWon)command).LeadId}/won"));
        commands.Register<MarkLeadAsLost, LeadOutcome>(new CommandRoute("POST", "/api/leads/{leadId}/lost",
            command => $"/api/leads/{((MarkLeadAsLost)command).LeadId}/lost"));
    }
}

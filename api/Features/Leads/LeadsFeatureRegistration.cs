using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Features.Leads.Commands;
using Jewel.JPMS.Api.Features.Leads.Queries;
using Jewel.JPMS.Contracts.Leads;
using Jewel.JPMS.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Jewel.JPMS.Api.Features.Leads;

public static class LeadsFeatureRegistration
{
    public static IServiceCollection AddLeadsFeature(this IServiceCollection services)
    {
        services.AddScoped<IQueryHandler<ListLeadsInPipeline, IReadOnlyList<Lead>>, ListLeadsInPipelineHandler>();
        services.AddScoped<IQueryHandler<GetBidDecisionForLead, BidDecision?>, GetBidDecisionForLeadHandler>();
        services.AddScoped<IQueryHandler<GetLeadQualification, QualificationAssessment?>, GetLeadQualificationHandler>();
        services.AddScoped<IQueryHandler<ListSiteVisitsForLead, IReadOnlyList<SiteVisit>>, ListSiteVisitsForLeadHandler>();
        services.AddScoped<IQueryHandler<ListInformationChaseItemsForLead, IReadOnlyList<InfoChaseItem>>, ListInformationChaseItemsForLeadHandler>();
        services.AddScoped<IQueryHandler<GetProposalForLead, Proposal?>, GetProposalForLeadHandler>();
        services.AddScoped<IQueryHandler<GetLeadOutcome, LeadOutcome?>, GetLeadOutcomeHandler>();

        RegisterCommand<CaptureLead, Lead, CaptureLeadHandler, CaptureLeadAuthorisation, CaptureLeadValidation>(services);
        RegisterCommand<UpdateLeadDetails, Lead, UpdateLeadDetailsHandler, UpdateLeadDetailsAuthorisation, UpdateLeadDetailsValidation>(services);
        RegisterCommand<RecordLeadQualificationScore, QualificationAssessment, RecordLeadQualificationScoreHandler, RecordLeadQualificationScoreAuthorisation, RecordLeadQualificationScoreValidation>(services);
        RegisterCommand<BookSiteVisit, SiteVisit, BookSiteVisitHandler, BookSiteVisitAuthorisation, BookSiteVisitValidation>(services);
        RegisterCommand<RecordSiteVisitNotes, SiteVisit, RecordSiteVisitNotesHandler, RecordSiteVisitNotesAuthorisation, RecordSiteVisitNotesValidation>(services);
        RegisterCommand<RecordInformationChaseItem, InfoChaseItem, RecordInformationChaseItemHandler, RecordInformationChaseItemAuthorisation, RecordInformationChaseItemValidation>(services);
        RegisterCommand<RecordBidDecision, BidDecision, RecordBidDecisionHandler, RecordBidDecisionAuthorisation, RecordBidDecisionValidation>(services);
        RegisterCommand<IssueProposal, Proposal, IssueProposalHandler, IssueProposalAuthorisation, IssueProposalValidation>(services);
        RegisterCommand<ReviseProposal, Proposal, ReviseProposalHandler, ReviseProposalAuthorisation, ReviseProposalValidation>(services);
        RegisterCommand<MarkLeadAsWon, LeadOutcome, MarkLeadAsWonHandler, MarkLeadAsWonAuthorisation, MarkLeadAsWonValidation>(services);
        RegisterCommand<MarkLeadAsLost, LeadOutcome, MarkLeadAsLostHandler, MarkLeadAsLostAuthorisation, MarkLeadAsLostValidation>(services);

        return services;
    }

    private static void RegisterCommand<TCommand, TResult, THandler, TAuthorisation, TValidation>(IServiceCollection services)
        where TCommand : Jewel.JPMS.Contracts.Cqrs.ICommand<TResult>
        where THandler : class, ICommandHandler<TCommand, TResult>
        where TAuthorisation : class
        where TValidation : class
    {
        services.AddScoped<ICommandHandler<TCommand, TResult>, THandler>();
        services.AddScoped<TAuthorisation>();
        services.AddScoped<TValidation>();
    }
}

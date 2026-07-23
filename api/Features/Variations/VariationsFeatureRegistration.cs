using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Features.Variations.Commands;
using Jewel.JPMS.Api.Features.Variations.Queries;
using Jewel.JPMS.Contracts.Variations;
using Jewel.JPMS.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Jewel.JPMS.Api.Features.Variations;

public static class VariationsFeatureRegistration
{
    public static IServiceCollection AddVariationsFeature(this IServiceCollection services)
    {
        services.AddScoped<IQueryHandler<GetVoqById, VariationOrderQuote?>, GetVoqByIdHandler>();
        services.AddScoped<IQueryHandler<GetVoqByRequest, VariationOrderQuote?>, GetVoqByRequestHandler>();
        services.AddScoped<IQueryHandler<ListVoqsForProject, IReadOnlyList<VariationOrderQuote>>, ListVoqsForProjectHandler>();
        services.AddScoped<IQueryHandler<ListBidPackagesForVoq, IReadOnlyList<BidPackage>>, ListBidPackagesForVoqHandler>();
        services.AddScoped<IQueryHandler<GetVariationOrderById, VariationOrder?>, GetVariationOrderByIdHandler>();
        services.AddScoped<IQueryHandler<GetVariationOrderByVoq, VariationOrder?>, GetVariationOrderByVoqHandler>();
        services.AddScoped<IQueryHandler<ListVariationOrdersForProject, IReadOnlyList<VariationOrder>>, ListVariationOrdersForProjectHandler>();

        services.AddScoped<ICommandHandler<CreateVoqFromRfq, VariationOrderQuote>, CreateVoqFromRfqHandler>();
        services.AddScoped<CreateVoqFromRfqAuthorisation>();
        services.AddScoped<CreateVoqFromRfqValidation>();

        services.AddScoped<ICommandHandler<PrepareVoqDraft, VoqDraftProposal>, PrepareVoqDraftHandler>();
        services.AddScoped<PrepareVoqDraftAuthorisation>();
        services.AddScoped<PrepareVoqDraftValidation>();

        services.AddScoped<ICommandHandler<AddBidPackageToVoq, BidPackage>, AddBidPackageToVoqHandler>();
        services.AddScoped<AddBidPackageToVoqAuthorisation>();
        services.AddScoped<AddBidPackageToVoqValidation>();

        services.AddScoped<ICommandHandler<SelectVoqTender, VariationOrderQuote>, SelectVoqTenderHandler>();
        services.AddScoped<SelectVoqTenderAuthorisation>();
        services.AddScoped<SelectVoqTenderValidation>();

        services.AddScoped<ICommandHandler<LinkVoqToRequest, VariationOrderQuote>, LinkVoqToRequestHandler>();
        services.AddScoped<LinkVoqToRequestAuthorisation>();
        services.AddScoped<LinkVoqToRequestValidation>();

        services.AddScoped<ICommandHandler<ApproveVariationOrderQuote, VariationOrder>, ApproveVariationOrderQuoteHandler>();
        services.AddScoped<ApproveVariationOrderQuoteAuthorisation>();
        services.AddScoped<ApproveVariationOrderQuoteValidation>();

        services.AddScoped<ICommandHandler<IssueVariationOrder, VariationOrder>, IssueVariationOrderHandler>();
        services.AddScoped<IssueVariationOrderAuthorisation>();
        services.AddScoped<IssueVariationOrderValidation>();

        services.AddScoped<ICommandHandler<CancelVariationOrder, VariationOrder>, CancelVariationOrderHandler>();
        services.AddScoped<CancelVariationOrderAuthorisation>();
        services.AddScoped<CancelVariationOrderValidation>();

        services.AddScoped<ICommandHandler<ReturnVoqToTendering, VariationOrderQuote>, ReturnVoqToTenderingHandler>();
        services.AddScoped<ReturnVoqToTenderingAuthorisation>();
        services.AddScoped<ReturnVoqToTenderingValidation>();

        services.AddScoped<ICommandHandler<ReviseVariationOrderValue, VariationOrder>, ReviseVariationOrderValueHandler>();
        services.AddScoped<ReviseVariationOrderValueAuthorisation>();
        services.AddScoped<ReviseVariationOrderValueValidation>();

        services.AddScoped<ICommandHandler<SetVoqStatus, VariationOrderQuote>, SetVoqStatusHandler>();
        services.AddScoped<SetVoqStatusAuthorisation>();
        services.AddScoped<SetVoqStatusValidation>();

        services.AddScoped<ICommandHandler<RevertVariationOrderToApproved, VariationOrder>, RevertVariationOrderToApprovedHandler>();
        services.AddScoped<RevertVariationOrderToApprovedAuthorisation>();
        services.AddScoped<RevertVariationOrderToApprovedValidation>();

        // Subcontractor variation requests (portal-raised; see subcontractor-crm-scope §6).
        services.AddScoped<ICommandHandler<AcceptVariationRequest, VariationOrderQuote>, AcceptVariationRequestHandler>();

        return services;
    }
}

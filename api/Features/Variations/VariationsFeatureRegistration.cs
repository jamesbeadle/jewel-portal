using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Features.Variations.Commands;
using Jewel.JPMS.Api.Features.Variations.Queries;
using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Contracts.Variations;
using Jewel.JPMS.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Jewel.JPMS.Api.Features.Variations;

public static class VariationsFeatureRegistration
{
    public static IServiceCollection AddVariationsFeature(this IServiceCollection services)
    {
        services.AddScoped<IQueryHandler<GetVoqByRequest, VariationOrder?>, GetVoqByRequestHandler>();
        services.AddScoped<IQueryHandler<ListBidPackagesForVoq, IReadOnlyList<BidPackage>>, ListBidPackagesForVoqHandler>();
        services.AddScoped<IQueryHandler<GetVariationOrderById, VariationOrder?>, GetVariationOrderByIdHandler>();
        services.AddScoped<IQueryHandler<ListVariationOrdersForProject, IReadOnlyList<VariationOrder>>, ListVariationOrdersForProjectHandler>();

        services.AddScoped<ICommandHandler<CreateVoqFromRfq, VariationOrder>, CreateVoqFromRfqHandler>();
        services.AddScoped<CreateVoqFromRfqAuthorisation>();
        services.AddScoped<CreateVoqFromRfqValidation>();

        services.AddScoped<ICommandHandler<CreateManualVariationOrder, VariationOrder>, CreateManualVariationOrderHandler>();
        services.AddScoped<CreateManualVariationOrderAuthorisation>();
        services.AddScoped<CreateManualVariationOrderValidation>();

        services.AddScoped<ICommandHandler<PrepareVoqDraft, VoqDraftProposal>, PrepareVoqDraftHandler>();
        services.AddScoped<PrepareVoqDraftAuthorisation>();
        services.AddScoped<PrepareVoqDraftValidation>();

        services.AddScoped<ICommandHandler<AddBidPackageToVoq, BidPackage>, AddBidPackageToVoqHandler>();
        services.AddScoped<AddBidPackageToVoqAuthorisation>();
        services.AddScoped<AddBidPackageToVoqValidation>();

        services.AddScoped<ICommandHandler<SelectVoqTender, VariationOrder>, SelectVoqTenderHandler>();
        services.AddScoped<SelectVoqTenderAuthorisation>();
        services.AddScoped<SelectVoqTenderValidation>();

        services.AddScoped<ICommandHandler<LinkVoqToRequest, VariationOrder>, LinkVoqToRequestHandler>();
        services.AddScoped<LinkVoqToRequestAuthorisation>();
        services.AddScoped<LinkVoqToRequestValidation>();

        services.AddScoped<ICommandHandler<ApproveVariationOrder, VariationOrder>, ApproveVariationOrderHandler>();
        services.AddScoped<ApproveVariationOrderAuthorisation>();
        services.AddScoped<ApproveVariationOrderValidation>();

        services.AddScoped<ICommandHandler<RejectVariationOrder, VariationOrder>, RejectVariationOrderHandler>();
        services.AddScoped<RejectVariationOrderAuthorisation>();
        services.AddScoped<RejectVariationOrderValidation>();

        services.AddScoped<ICommandHandler<ReturnVariationOrderToQuoting, VariationOrder>, ReturnVariationOrderToQuotingHandler>();
        services.AddScoped<ReturnVariationOrderToQuotingAuthorisation>();
        services.AddScoped<ReturnVariationOrderToQuotingValidation>();

        services.AddScoped<ICommandHandler<ReviseVariationOrderValue, VariationOrder>, ReviseVariationOrderValueHandler>();
        services.AddScoped<ReviseVariationOrderValueAuthorisation>();
        services.AddScoped<ReviseVariationOrderValueValidation>();

        services.AddScoped<ICommandHandler<SetVariationOrderStatus, VariationOrder>, SetVariationOrderStatusHandler>();
        services.AddScoped<SetVariationOrderStatusAuthorisation>();
        services.AddScoped<SetVariationOrderStatusValidation>();

        services.AddScoped<ICommandHandler<DeleteVariationOrder, Acknowledgement>, DeleteVariationOrderHandler>();
        services.AddScoped<DeleteVariationOrderAuthorisation>();
        services.AddScoped<DeleteVariationOrderValidation>();

        // Subcontractor variation requests (portal-raised; see subcontractor-crm-scope §6).
        services.AddScoped<ICommandHandler<AcceptVariationRequest, VariationOrder>, AcceptVariationRequestHandler>();

        return services;
    }
}

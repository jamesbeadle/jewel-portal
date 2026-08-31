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
        services.AddScoped<IQueryHandler<GetVariationOrderById, VariationOrder?>, GetVariationOrderByIdHandler>();
        services.AddScoped<IQueryHandler<ListVariationOrdersForProject, IReadOnlyList<VariationOrder>>, ListVariationOrdersForProjectHandler>();
        services.AddScoped<IQueryHandler<GetVariationOrderDocument, VariationDocumentFile?>, GetVariationOrderDocumentHandler>();
        services.AddScoped<IQueryHandler<ListVariationOrderMessages, IReadOnlyList<VariationOrderMessage>>, ListVariationOrderMessagesHandler>();

        services.AddScoped<ICommandHandler<PostVariationOrderMessage, VariationOrderMessage>, PostVariationOrderMessageHandler>();
        services.AddScoped<PostVariationOrderMessageAuthorisation>();
        services.AddScoped<PostVariationOrderMessageValidation>();

        services.AddScoped<ICommandHandler<CreateVoqFromRfq, VariationOrder>, CreateVoqFromRfqHandler>();
        services.AddScoped<CreateVoqFromRfqAuthorisation>();
        services.AddScoped<CreateVoqFromRfqValidation>();

        services.AddScoped<ICommandHandler<CreateManualVariationOrder, VariationOrder>, CreateManualVariationOrderHandler>();
        services.AddScoped<CreateManualVariationOrderAuthorisation>();
        services.AddScoped<CreateManualVariationOrderValidation>();

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

        services.AddScoped<ICommandHandler<ReviseVariationOrderLines, VariationOrder>, ReviseVariationOrderLinesHandler>();
        services.AddScoped<ReviseVariationOrderLinesAuthorisation>();
        services.AddScoped<ReviseVariationOrderLinesValidation>();

        // The agreed build-up staged before approval (2026-08-25).
        services.AddScoped<ICommandHandler<StageVariationOrderBuildUp, VariationOrder>, StageVariationOrderBuildUpHandler>();
        services.AddScoped<StageVariationOrderBuildUpAuthorisation>();
        services.AddScoped<StageVariationOrderBuildUpValidation>();

        services.AddScoped<ICommandHandler<SetVariationOrderStatus, VariationOrder>, SetVariationOrderStatusHandler>();
        services.AddScoped<SetVariationOrderStatusAuthorisation>();
        services.AddScoped<SetVariationOrderStatusValidation>();

        services.AddScoped<ICommandHandler<RenameVariationOrder, VariationOrder>, RenameVariationOrderHandler>();
        services.AddScoped<RenameVariationOrderAuthorisation>();
        services.AddScoped<RenameVariationOrderValidation>();

        services.AddScoped<ICommandHandler<SetVariationOrderEstimate, VariationOrder>, SetVariationOrderEstimateHandler>();
        services.AddScoped<SetVariationOrderEstimateAuthorisation>();
        services.AddScoped<SetVariationOrderEstimateValidation>();

        services.AddScoped<ICommandHandler<UpdateVariationOrderNarratives, VariationOrder>, UpdateVariationOrderNarrativesHandler>();
        services.AddScoped<UpdateVariationOrderNarrativesAuthorisation>();
        services.AddScoped<UpdateVariationOrderNarrativesValidation>();

        services.AddScoped<ICommandHandler<DeleteVariationOrder, Acknowledgement>, DeleteVariationOrderHandler>();
        services.AddScoped<DeleteVariationOrderAuthorisation>();
        services.AddScoped<DeleteVariationOrderValidation>();

        // Subcontractor variation requests (portal-raised; see subcontractor-crm-scope §6).
        services.AddScoped<ICommandHandler<AcceptVariationRequest, VariationOrder>, AcceptVariationRequestHandler>();

        return services;
    }
}

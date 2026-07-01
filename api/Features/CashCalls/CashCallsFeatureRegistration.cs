using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Features.CashCalls.Commands;
using Jewel.JPMS.Api.Features.CashCalls.Queries;
using Jewel.JPMS.Contracts.CashCalls;
using Jewel.JPMS.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Jewel.JPMS.Api.Features.CashCalls;

public static class CashCallsFeatureRegistration
{
    public static IServiceCollection AddCashCallsFeature(this IServiceCollection services)
    {
        services.AddScoped<IQueryHandler<ListCashCallsForProject, IReadOnlyList<CashCall>>, ListCashCallsForProjectHandler>();
        services.AddScoped<IQueryHandler<GetProjectCashCallSummary, ProjectCashCallSummary>, GetProjectCashCallSummaryHandler>();

        services.AddScoped<ICommandHandler<CreateCashCall, CashCall>, CreateCashCallHandler>();
        services.AddScoped<CreateCashCallAuthorisation>();
        services.AddScoped<CreateCashCallValidation>();

        services.AddScoped<ICommandHandler<IssueClientInvoice, CashCall>, IssueClientInvoiceHandler>();
        services.AddScoped<IssueClientInvoiceAuthorisation>();
        services.AddScoped<IssueClientInvoiceValidation>();

        services.AddScoped<ICommandHandler<RecordCashCallReceipt, CashCall>, RecordCashCallReceiptHandler>();
        services.AddScoped<RecordCashCallReceiptAuthorisation>();
        services.AddScoped<RecordCashCallReceiptValidation>();

        return services;
    }
}

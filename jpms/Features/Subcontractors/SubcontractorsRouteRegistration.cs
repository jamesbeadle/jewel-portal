using Jewel.JPMS.Contracts.Subcontractors;
using Jewel.JPMS.Cqrs;
using Jewel.JPMS.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Jewel.JPMS.Features.Subcontractors;

public static class SubcontractorsRouteRegistration
{
    public static IServiceCollection AddSubcontractorsReadModels(this IServiceCollection services)
    {
        services.AddScoped<SubcontractorsReadModel>();
        services.AddScoped<TradesReadModel>();
        return services;
    }

    public static void RegisterSubcontractorsRoutes(QueryRouteTable queries, CommandRouteTable commands)
    {
        queries.Register<ListSubcontractors, IReadOnlyList<Subcontractor>>(QueryRoute.Static("/api/subcontractors"));

        queries.Register<ListTrades, IReadOnlyList<Trade>>(QueryRoute.Static("/api/trades"));

        commands.Register<AddTrade, Trade>(CommandRoute.Post("/api/trades"));

        queries.Register<ListComplianceDocumentsForSubcontractor, IReadOnlyList<ComplianceDocument>>(
            new QueryRoute("/api/subcontractors/{subcontractorId}/compliance",
                query => $"/api/subcontractors/{((ListComplianceDocumentsForSubcontractor)query).SubcontractorId}/compliance"));

        queries.Register<GetSubcontractorStatement, SubcontractorStatement>(
            new QueryRoute("/api/subcontractors/{subcontractorId}/statement",
                query => $"/api/subcontractors/{((GetSubcontractorStatement)query).SubcontractorId}/statement"));

        commands.Register<PrepareSubcontractorStatementEmailDraft, SubcontractorStatementEmailDraft>(
            new CommandRoute("POST", "/api/subcontractors/{subcontractorId}/statement/draft-email",
                command => $"/api/subcontractors/{((PrepareSubcontractorStatementEmailDraft)command).SubcontractorId}/statement/draft-email"));

        commands.Register<AddSubcontractorToDirectory, Subcontractor>(CommandRoute.Post("/api/subcontractors"));

        commands.Register<UpdateSubcontractor, Subcontractor>(
            new CommandRoute("PUT", "/api/subcontractors/{subcontractorId}",
                command => $"/api/subcontractors/{((UpdateSubcontractor)command).SubcontractorId}"));

        commands.Register<UploadComplianceDocument, ComplianceDocument>(
            new CommandRoute("POST", "/api/subcontractors/{subcontractorId}/compliance",
                command => $"/api/subcontractors/{((UploadComplianceDocument)command).SubcontractorId}/compliance"));
    }
}

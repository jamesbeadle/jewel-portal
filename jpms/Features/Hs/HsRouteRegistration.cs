using Jewel.JPMS.Contracts.Hs;
using Jewel.JPMS.Cqrs;
using Jewel.JPMS.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Jewel.JPMS.Features.Hs;

public static class HsRouteRegistration
{
    public static IServiceCollection AddHsReadModels(this IServiceCollection services)
    {
        services.AddScoped<HsRecordsReadModel>();
        return services;
    }

    public static void RegisterHsRoutes(QueryRouteTable queries, CommandRouteTable commands)
    {
        queries.Register<ListHsRecords, IReadOnlyList<HsRecord>>(QueryRoute.Static("/api/hs-records"));
        queries.Register<ListAttendanceForHsRecord, IReadOnlyList<HsRecordAttendance>>(
            new QueryRoute("/api/hs-records/{hsRecordId}/attendance",
                query => $"/api/hs-records/{((ListAttendanceForHsRecord)query).HsRecordId}/attendance"));

        commands.Register<LogHsRecord, HsRecord>(CommandRoute.Post("/api/hs-records"));
        commands.Register<UpdateHsRecord, HsRecord>(
            new CommandRoute("PUT", "/api/hs-records/{hsRecordId}",
                command => $"/api/hs-records/{((UpdateHsRecord)command).HsRecordId}"));
        commands.Register<RecordAttendanceForHsRecord, HsRecordAttendance>(
            new CommandRoute("POST", "/api/hs-records/{hsRecordId}/attendance",
                command => $"/api/hs-records/{((RecordAttendanceForHsRecord)command).HsRecordId}/attendance"));
    }
}

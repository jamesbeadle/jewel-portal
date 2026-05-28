using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Features.Hs.Commands;
using Jewel.JPMS.Api.Features.Hs.Queries;
using Jewel.JPMS.Contracts.Hs;
using Jewel.JPMS.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Jewel.JPMS.Api.Features.Hs;

public static class HsFeatureRegistration
{
    public static IServiceCollection AddHsFeature(this IServiceCollection services)
    {
        services.AddScoped<IQueryHandler<ListHsRecords, IReadOnlyList<HsRecord>>, ListHsRecordsHandler>();
        services.AddScoped<IQueryHandler<ListAttendanceForHsRecord, IReadOnlyList<HsRecordAttendance>>, ListAttendanceForHsRecordHandler>();

        services.AddScoped<ICommandHandler<LogHsRecord, HsRecord>, LogHsRecordHandler>();
        services.AddScoped<LogHsRecordAuthorisation>();
        services.AddScoped<LogHsRecordValidation>();

        services.AddScoped<ICommandHandler<UpdateHsRecord, HsRecord>, UpdateHsRecordHandler>();
        services.AddScoped<UpdateHsRecordAuthorisation>();
        services.AddScoped<UpdateHsRecordValidation>();

        services.AddScoped<ICommandHandler<RecordAttendanceForHsRecord, HsRecordAttendance>, RecordAttendanceForHsRecordHandler>();
        services.AddScoped<RecordAttendanceForHsRecordAuthorisation>();
        services.AddScoped<RecordAttendanceForHsRecordValidation>();

        return services;
    }
}

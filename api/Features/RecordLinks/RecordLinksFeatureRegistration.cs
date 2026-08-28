using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Features.RecordLinks.Commands;
using Jewel.JPMS.Api.Features.RecordLinks.Providers;
using Jewel.JPMS.Api.Features.RecordLinks.Queries;
using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Contracts.RecordLinks;
using Jewel.JPMS.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Jewel.JPMS.Api.Features.RecordLinks;

// The record-agnostic email-link layer: providers per record type, the registry that resolves them,
// the generic link command + list query, and the live email reader. Adding a new linkable record type
// is one AddScoped<ILinkableRecordProvider, …> line here (plus its own list/find implementation).
public static class RecordLinksFeatureRegistration
{
    public static IServiceCollection AddRecordLinksFeature(this IServiceCollection services)
    {
        // One provider per record type. Registered as the interface so the registry collects them all.
        services.AddScoped<ILinkableRecordProvider, RequestLinkProvider>();
        services.AddScoped<ILinkableRecordProvider, BidPackageInviteLinkProvider>();
        services.AddScoped<ILinkableRecordProvider, WorkOrderLinkProvider>();
        services.AddScoped<ILinkableRecordProvider, CostCentreLinkProvider>();
        services.AddScoped<ILinkableRecordProvider, SchedulingLinkProvider>();
        services.AddScoped<ILinkableRecordProvider, TodoLinkProvider>();
        services.AddScoped<ILinkableRecordProvider, LadLinkProvider>();
        services.AddScoped<ILinkableRecordProvider, VariationOrderLinkProvider>();
        services.AddScoped<ILinkableRecordProvider, VariationOrderQuoteLinkProvider>();
        services.AddScoped<ILinkableRecordProvider, DefectLinkProvider>();
        services.AddScoped<ILinkableRecordProvider, SubcontractorCommsLinkProvider>();
        services.AddScoped<ILinkableRecordProvider, SupplierCommsLinkProvider>();
        services.AddScoped<ILinkableRecordProvider, InternalCommsLinkProvider>();
        services.AddScoped<ILinkableRecordProvider, ValuationReportSnapshotLinkProvider>();
        services.AddScoped<ILinkableRecordProvider, TenderEnquiryLinkProvider>();
        services.AddScoped<ILinkableRecordProvider, CalendarEventLinkProvider>();
        services.AddScoped<ILinkableRecordProvider, BuildingControlCaseLinkProvider>();
        services.AddScoped<ILinkableRecordProvider, BuildingControlInspectionLinkProvider>();

        services.AddScoped<RecordProviderRegistry>();
        services.AddScoped<RecordEmailReader>();
        // Tags an email's whole conversation (not just the clicked message) to a record at triage
        // time, and answers the queue's "thread already linked?" hint lookup. Later arrivals are
        // never auto-tagged — they queue for their own triage decision.
        services.AddScoped<RecordThreadTagger>();

        services.AddScoped<IQueryHandler<ListLinkableRecords, IReadOnlyList<LinkableRecord>>, ListLinkableRecordsHandler>();
        services.AddScoped<IQueryHandler<ListSchedulingEmails, IReadOnlyList<MailboxMessage>>, ListSchedulingEmailsHandler>();
        services.AddScoped<IQueryHandler<GetProgrammeEmailDetail, MailboxMessageDetail>, GetProgrammeEmailDetailHandler>();
        services.AddScoped<IQueryHandler<ListRecordEmails, IReadOnlyList<MailboxMessage>>, ListRecordEmailsHandler>();
        // The replies a record page is blind to: newer thread members not yet tagged to it.
        services.AddScoped<IQueryHandler<ListUnfiledReplies, IReadOnlyList<MailboxMessage>>, ListUnfiledRepliesHandler>();
        services.AddScoped<IQueryHandler<SearchMailboxMessages, IReadOnlyList<MailboxMessage>>, SearchMailboxMessagesHandler>();
        services.AddScoped<IQueryHandler<ResolveRecordTags, IReadOnlyList<LinkableRecord>>, ResolveRecordTagsHandler>();
        services.AddScoped<IQueryHandler<ListRecordActivity, IReadOnlyList<RecordActivitySummary>>, ListRecordActivityHandler>();
        services.AddScoped<IQueryHandler<ListProjectCommunications, ProjectCommunicationsPage>, ListProjectCommunicationsHandler>();
        services.AddScoped<ICommandHandler<LinkMessageToRecord, Acknowledgement>, LinkMessageToRecordHandler>();
        // Gate classes for the connector's file_email_to_record action (2026-08-28) — the HTTP
        // family keeps its shared Gate; both read TriageRoles.AllowedToTriage.
        services.AddScoped<LinkMessageToRecordAuthorisation>();
        services.AddScoped<LinkMessageToRecordValidation>();
        // Connector "File them all here" (file_unfiled_replies action): server-side twin of the
        // record pages' unfiled-replies banner button. No HTTP endpoint.
        services.AddScoped<ICommandHandler<FileUnfiledReplies, FileUnfiledRepliesResult>, FileUnfiledRepliesHandler>();
        services.AddScoped<FileUnfiledRepliesAuthorisation>();
        services.AddScoped<FileUnfiledRepliesValidation>();
        services.AddScoped<ICommandHandler<PrepareProgrammeReplyDraft, ProgrammeReplyDraft>, PrepareProgrammeReplyDraftHandler>();

        return services;
    }
}

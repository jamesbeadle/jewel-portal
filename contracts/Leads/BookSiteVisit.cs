using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Leads;

public sealed record BookSiteVisit(
    string LeadId,
    DateTimeOffset ScheduledAt,
    IReadOnlyList<string> AttendeeEmails) : ICommand<SiteVisit>;

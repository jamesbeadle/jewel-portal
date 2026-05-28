using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Leads;

public sealed record RecordSiteVisitNotes(
    string SiteVisitId,
    string Notes,
    int PhotoCount,
    bool IsComplete) : ICommand<SiteVisit>;

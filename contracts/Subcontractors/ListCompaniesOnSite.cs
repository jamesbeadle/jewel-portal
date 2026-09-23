using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Subcontractors;

/// <summary>
/// The companies working on a Jewel site: those holding a work order that has been released and not
/// yet completed, on a project that is not finished, with the projects it is on. The compliance
/// register reads it beside the documents, so a company whose insurance has lapsed while it is on
/// site is seen on the day the certificate lapses, not on the day somebody notices.
/// </summary>
public sealed record ListCompaniesOnSite() : IQuery<IReadOnlyList<CompanyOnSite>>;

public sealed record CompanyOnSite(string SubcontractorId, IReadOnlyList<string> ProjectNames);

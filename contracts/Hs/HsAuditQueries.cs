using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Hs;

/// <summary>The project's audits, newest first — the H&S tab's Audits list.</summary>
public sealed record ListHsAuditsForProject(string ProjectId) : IQuery<IReadOnlyList<HsAudit>>;

/// <summary>One audit with every item of its framework in template order — the form page.</summary>
public sealed record HsAuditView(HsAudit Audit, IReadOnlyList<HsAuditItem> Items);

public sealed record GetHsAudit(string HsAuditId) : IQuery<HsAuditView>;

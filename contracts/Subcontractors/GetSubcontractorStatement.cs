using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Subcontractors;

/// <summary>
/// The subcontractor's statement of account across every project: each work order they hold
/// (grouped by project) with the Xero purchase invoices claimed against it, plus per-order and
/// overall ordered / invoiced / remaining balances. The single source behind the statement PDF,
/// the statement email and the directory page's account view. Released and Complete orders are
/// always included; Draft and Cancelled orders appear only when invoices are already linked to
/// them, so claimed money never silently drops off the statement.
/// </summary>
public sealed record GetSubcontractorStatement(string SubcontractorId) : IQuery<SubcontractorStatement>;

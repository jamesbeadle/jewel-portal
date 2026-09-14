using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Commercial;

/// <summary>
/// One supplier's account on one project, read the way the accountant reconciles it (2026-09-14,
/// the accountant's ask): the live work orders the supplier holds on the project with their priced
/// lines, what has been invoiced and linked against each, what Xero has settled and what is left
/// to invoice — then every purchase invoice received from the supplier for the project, linked to
/// an order or not, with its CIS labour / materials split, its Xero status and the order(s) it is
/// matched to. The two halves reconcile: received less ordered is the over-invoice, when there is
/// one. The single source behind the supplier account modal on the Work orders tab, its PDF and
/// the connector's get_project_supplier_account.
///
/// <para>Distinct from <see cref="Subcontractors.GetSubcontractorStatement"/>, which is
/// correspondence TO the supplier across every project and never shows payment or unmatched
/// bills. This one is internal: it exists so the finance director can show the managing director
/// where a supplier stands before an over-invoice is paid.</para>
/// </summary>
public sealed record GetProjectSupplierAccount(string ProjectId, string SubcontractorId)
    : IQuery<ProjectSupplierAccount>;

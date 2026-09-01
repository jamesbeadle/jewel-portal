using Jewel.JPMS.Api.Features.Boq.Commands;
using Jewel.JPMS.Api.Features.Lads;
using Jewel.JPMS.Api.Features.Lads.Commands;
using Jewel.JPMS.Api.Features.Retention.Commands;
using Jewel.JPMS.Api.Features.ValuationInvoices;
using Jewel.JPMS.Api.Features.ValuationInvoices.Commands;
using Jewel.JPMS.Api.Features.Variations;
using Jewel.JPMS.Api.Features.Variations.Commands;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Boq;
using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Contracts.Lads;
using Jewel.JPMS.Contracts.Retention;
using Jewel.JPMS.Contracts.ValuationInvoices;
using Jewel.JPMS.Contracts.Variations;

namespace Jewel.JPMS.Api.Features.Ai.Tools.Actions;

/// <summary>Variation, valuation-invoice, BoQ, retention and LADs commands as connector actions.
/// Mirrors Features/Variations, Features/ValuationInvoices, Features/Boq, Features/Retention and
/// Features/Lads — each entry's VisibleTo copies its Authorisation class's role set, and the
/// stamps copy exactly what the endpoint stamps server-side.</summary>
internal sealed partial class VariationsAndValuationsActions : IAiActionSource
{
    // The BoQ authorisations keep their role sets as private fields; these replicate them
    // role-for-role (AddBoqLineAuthorisation / UpdateBoqLineAuthorisation /
    // RemoveBoqLineAuthorisation, and SignOffBoqForProjectAuthorisation).
    private static readonly RoleSet BoqEditors =
        RoleSet.Of(JpmsRoles.Director, JpmsRoles.ProjectManager, JpmsRoles.Estimator);
    private static readonly RoleSet BoqSignOffDirectors = RoleSet.Of(JpmsRoles.Director);

    // Replicates the private field shared in spirit by SetProjectRetentionAuthorisation and
    // ConfirmRetentionReleaseAuthorisation — directors and the finance director.
    private static readonly RoleSet RetentionDirectors =
        RoleSet.Of(JpmsRoles.Director, JpmsRoles.FinanceDirector);

    public IEnumerable<AiAction> Build() =>
        VariationsActions()
            .Concat(ValuationInvoicesActions())
            .Concat(BoqActions())
            .Concat(RetentionActions())
            .Concat(LadsActions());

    // Skipped: AcceptVariationRequest — dispatches ICommandHandler<AcceptVariationRequest, VariationOrder>,
    //          but the endpoint has no Authorisation class (an inline VariationRoles.AllowedToManageVariations
    //          check) and no Validation class, so the AiAction pattern's required AuthorisationType cannot be
    //          satisfied without inventing a class that does not exist.
    // Skipped: RejectVariationRequestEndpoint — no command dispatch: the endpoint mutates the
    //          SubcontractorVariationRequest row directly through JpmsContext (no ICommandHandler,
    //          no Authorisation/Validation classes).
}

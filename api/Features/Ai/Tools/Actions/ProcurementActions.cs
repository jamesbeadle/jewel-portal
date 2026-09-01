using Jewel.JPMS.Api.Features.Procurement.Commands;
using Jewel.JPMS.Contracts.Procurement;

namespace Jewel.JPMS.Api.Features.Ai.Tools.Actions;

/// <summary>Procurement commands (bid packages, tenders, quotes, work orders) as connector
/// actions. Mirrors Features/Procurement/Commands — each entry's VisibleTo copies its
/// Authorisation class's role set (every procurement authorisation keeps its set private, so the
/// sets are replicated below with the identical roles), and the stamps copy exactly what the
/// endpoint stamps server-side. Follows CalendarActions, THE EXEMPLAR FILE for the pattern.</summary>
internal sealed partial class ProcurementActions : IAiActionSource
{
    // Mirrors CreateBidPackageAuthorisation / AddBidPackageLineItemsAuthorisation /
    // DeleteBidPackageAuthorisation / SuggestBidPackagesAuthorisation /
    // SetBidPackageLineItemCoverageAuthorisation (all declare this same set privately).
    private static readonly RoleSet PackageCreators = RoleSet.Of(
        JpmsRoles.Director, JpmsRoles.ProjectManager,
        JpmsRoles.Estimator, JpmsRoles.OfficeComplianceCoordinator, JpmsRoles.OfficeAdmin);

    // Mirrors the tender-administration gates: CreateBidPackageFromMessageAuthorisation,
    // InviteSubcontractorsToBidPackageAuthorisation, DeclineBidPackageRecipientAuthorisation,
    // RemoveBidPackageRecipientAuthorisation, PrepareBidPackageInviteDraftAuthorisation,
    // PrepareWorkOrderEmailDraftAuthorisation, ExtractTenderFromMessageAuthorisation,
    // RecordTenderResponseAuthorisation, SaveExtractedQuoteAuthorisation,
    // SetBidPackageDrawingsAuthorisation, SetBidPackageLineItemsAuthorisation,
    // UpdateBidPackageScopeAuthorisation, UpdateWorkOrderAuthorisation.
    private static readonly RoleSet PackageAdministrators = RoleSet.Of(
        JpmsRoles.Director, JpmsRoles.ProjectManager,
        JpmsRoles.OfficeComplianceCoordinator, JpmsRoles.OfficeAdmin);

    // Mirrors CloseBidPackageAuthorisation / ReopenBidPackageAuthorisation.
    private static readonly RoleSet PackageClosers =
        RoleSet.Of(JpmsRoles.Director, JpmsRoles.ProjectManager);

    // Mirrors ReviseQuoteAuthorisation / SubmitQuoteForBidPackageAuthorisation.
    private static readonly RoleSet QuoteWriters = RoleSet.Of(
        JpmsRoles.Director, JpmsRoles.ProjectManager,
        JpmsRoles.OfficeComplianceCoordinator, JpmsRoles.OfficeAdmin, JpmsRoles.Subcontractor);

    // Mirrors CreateManualWorkOrderAuthorisation / CreateWorkOrderFromMessageAuthorisation /
    // ApproveWorkOrderAuthorisation / RejectWorkOrderAuthorisation /
    // DeleteDraftWorkOrderAuthorisation / RecodeWorkOrderLineAuthorisation.
    private static readonly RoleSet WorkOrderRaisers = RoleSet.Of(
        Role.Admin, JpmsRoles.Director, JpmsRoles.FinanceDirector,
        JpmsRoles.ProjectManager, JpmsRoles.Estimator);

    // Mirrors CancelWorkOrderAuthorisation — a directors' money decision.
    private static readonly RoleSet WorkOrderCancellers =
        RoleSet.Of(Role.Admin, JpmsRoles.Director, JpmsRoles.FinanceDirector);

    // Mirrors SendWorkOrderPoEmailAuthorisation.
    private static readonly RoleSet PoEmailSenders = RoleSet.Of(
        Role.Admin, JpmsRoles.Director, JpmsRoles.FinanceDirector, JpmsRoles.ProjectManager,
        JpmsRoles.Estimator, JpmsRoles.OfficeComplianceCoordinator, JpmsRoles.OfficeAdmin);

    public IEnumerable<AiAction> Build() =>
        BidPackagesActions()
            .Concat(PackageScopeActions())
            .Concat(InvitesActions())
            .Concat(TendersActions())
            .Concat(WorkOrdersActions());

    // Skipped: UpdateManualWorkOrder — the endpoint overwrites EditorMayEditAnyOrder server-side
    //   from the signed-in user's roles (MD/FD/Admin); the action gateway can only stamp emails and
    //   names, so the flag would appear in the model-facing schema and let any caller grant
    //   themselves the directors-only power to edit awarded/variation/seeded orders.
    // Skipped: SendBidPackageInvite — no Authorisation/Validation classes: the endpoint
    //   (BidPackageInviteComposerEndpoints) checks a private inline RoleSet, and AiAction requires
    //   an authorisation class resolvable from DI. (It also SENDS EMAIL to every invited
    //   subcontractor — deliberately left in the portal.)
    // Skipped: SaveBidPackageInviteComposerDraft — same endpoint, same inline-RoleSet shape: no
    //   Authorisation/Validation classes to declare.
    // Skipped: IssueWorkOrderForVariationOrder — no Authorisation/Validation classes: the endpoint
    //   checks a private inline RoleSet and builds the command from the route + session inline.
    // Skipped: RemoveBidPackageAttachment — no Authorisation/Validation classes: the attachments
    //   endpoint (BidPackageAttachmentEndpoints) checks a private inline RoleSet.
    // Skipped: RemoveWorkOrderAttachment — no Authorisation/Validation classes: the attachments
    //   endpoint (WorkOrderAttachmentEndpoints) checks a private inline RoleSet.
    // Skipped: UploadBidPackageAttachments / UploadWorkOrderAttachments (endpoints) — multipart
    //   file upload (IFormFile), no command dispatch.
    // Skipped: UploadCompanyTenderTerms (endpoint) — multipart file upload of the company terms
    //   PDF, no command dispatch.
}

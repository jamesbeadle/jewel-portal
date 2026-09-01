using Jewel.JPMS.Api.Features.Procurement.Commands;
using Jewel.JPMS.Contracts.Procurement;

namespace Jewel.JPMS.Api.Features.Ai.Tools.Actions;

internal sealed partial class ProcurementActions
{
    private static IEnumerable<AiAction> BidPackagesActions() => new AiAction[]
    {
        // ---- Bid packages -------------------------------------------------------------------

        new AiAction(
            Name: "create_bid_package",
            Area: "Procurement",
            Description: "Creates a new Draft bid package (tender package) on a project — a scope of "
                + "work to put out to subcontractors. Nothing is sent to anyone. Returns the created "
                + "package.",
            CommandType: typeof(CreateBidPackage),
            ResultType: typeof(BidPackage),
            AuthorisationType: typeof(CreateBidPackageAuthorisation),
            ValidationType: typeof(CreateBidPackageValidation),
            VisibleTo: PackageCreators,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            Notes: "projectId comes from list_projects. ownerEmail is carried in the request over "
                + "HTTP too — pass the signed-in user's email unless the user names another owner. "
                + "Set materialsApplicable true when the invite should ask each subcontractor "
                + "whether they will supply their own materials."),

        new AiAction(
            Name: "create_bid_package_from_message",
            Area: "Procurement",
            Description: "Creates a Draft bid package on a project from a tagged mailbox message and "
                + "links the originating email (and, by default, the thread behind it) to the new "
                + "package via the shared record-link tag. Nothing is sent to anyone. Returns the "
                + "created package.",
            CommandType: typeof(CreateBidPackageFromMessage),
            ResultType: typeof(BidPackage),
            AuthorisationType: typeof(CreateBidPackageFromMessageAuthorisation),
            ValidationType: typeof(CreateBidPackageFromMessageValidation),
            VisibleTo: PackageAdministrators,
            EmailStamps: new[] { "OwnerEmail" },
            NameStamps: Array.Empty<string>(),
            Notes: "messageId is a mailbox message id from the triage queue, not a request id. "
                + "projectId comes from list_projects. Filing under Subcontractor as well as a "
                + "pathway the thread already carries is refused unless allowCrossPathway is true — "
                + "only pass it after the user confirms."),

        new AiAction(
            Name: "update_bid_package_scope",
            Area: "Procurement",
            Description: "Updates a bid package's header — title, trade, status, owner, materials "
                + "flag and (optionally) specification summary. The whole editable surface travels "
                + "together, so send current values for anything that should not change. Returns the "
                + "updated package.",
            CommandType: typeof(UpdateBidPackageScope),
            ResultType: typeof(BidPackage),
            AuthorisationType: typeof(UpdateBidPackageScopeAuthorisation),
            ValidationType: typeof(UpdateBidPackageScopeValidation),
            VisibleTo: PackageAdministrators,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            Notes: "bidPackageId comes from list_bid_packages; read the current values first "
                + "(get_bid_package_context) and carry them forward. specificationSummary null "
                + "means leave unchanged."),

        new AiAction(
            Name: "delete_bid_package",
            Area: "Procurement",
            Description: "PERMANENTLY deletes a bid package and everything under it — invite rows, "
                + "line items, quotes and their lines, tender-document attachments and drawing "
                + "links. There is no undo. Tagged emails stay in the mailbox. Refused for an "
                + "Awarded package or while any work order references it.",
            CommandType: typeof(DeleteBidPackage),
            ResultType: typeof(Acknowledgement),
            AuthorisationType: typeof(DeleteBidPackageAuthorisation),
            ValidationType: typeof(DeleteBidPackageValidation),
            VisibleTo: PackageCreators,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            RequiresConfirmation: true,
            Notes: "Deletion is for packages that should never have existed; close_bid_package is "
                + "the polite no-winner ending for a real tender. Confirm with the user, naming the "
                + "package, before calling. bidPackageId comes from list_bid_packages."),

        new AiAction(
            Name: "close_bid_package",
            Area: "Procurement",
            Description: "Ends a bid package's tender process without picking a winner (all "
                + "tenderers declined, works re-scoped, package lapsed): sets the package Closed and "
                + "stamps ClosedAt. An Awarded package cannot be closed. Returns the updated "
                + "package.",
            CommandType: typeof(CloseBidPackage),
            ResultType: typeof(BidPackage),
            AuthorisationType: typeof(CloseBidPackageAuthorisation),
            ValidationType: typeof(CloseBidPackageValidation),
            VisibleTo: PackageClosers,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            Notes: "Reversible via reopen_bid_package. Confirm with the user before calling. "
                + "bidPackageId comes from list_bid_packages."),

        new AiAction(
            Name: "reopen_bid_package",
            Area: "Procurement",
            Description: "Puts a Closed bid package back in play: clears ClosedAt and restores the "
                + "status the package's data implies (QuotesReceived when it holds any tender, "
                + "Inviting when subcontractors were invited, Draft otherwise). Only a Closed "
                + "package can be reopened. Returns the updated package.",
            CommandType: typeof(ReopenBidPackage),
            ResultType: typeof(BidPackage),
            AuthorisationType: typeof(ReopenBidPackageAuthorisation),
            ValidationType: typeof(ReopenBidPackageValidation),
            VisibleTo: PackageClosers,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            Notes: "bidPackageId comes from list_bid_packages."),

        new AiAction(
            Name: "suggest_bid_packages",
            Area: "Procurement",
            Description: "Asks the portal's AI to read the project's live valuation report and "
                + "propose bid packages worth tendering for the remaining works, grouped by trade. "
                + "Nothing is created — the result is a list of proposals the user picks from "
                + "(create_bid_package makes a real one).",
            CommandType: typeof(SuggestBidPackages),
            ResultType: typeof(BidPackageSuggestionResult),
            AuthorisationType: typeof(SuggestBidPackagesAuthorisation),
            ValidationType: typeof(SuggestBidPackagesValidation),
            VisibleTo: PackageCreators,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            Notes: "projectId comes from list_projects. model is an AI tier key (haiku / sonnet / "
                + "opus / fable); unknown keys degrade to the cheap tier. If the result comes back "
                + "isComplete false, re-send the SAME command with the returned partialText to "
                + "continue the answer."),

    };
}

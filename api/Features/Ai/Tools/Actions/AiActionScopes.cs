using Jewel.JPMS.Api.Features.ArchitectInstructions;
using Jewel.JPMS.Api.Features.Closeout;
using Jewel.JPMS.Api.Features.Forms.Office.Submissions;
using Jewel.JPMS.Api.Features.Procurement;
using Jewel.JPMS.Api.Features.Requests;
using Jewel.JPMS.Api.Features.Variations;
using Jewel.JPMS.Contracts.ArchitectInstructions;
using Jewel.JPMS.Contracts.Closeout;
using Jewel.JPMS.Contracts.Forms;
using Jewel.JPMS.Contracts.Procurement;
using Jewel.JPMS.Contracts.Requests;
using Jewel.JPMS.Contracts.Variations;

namespace Jewel.JPMS.Api.Features.Ai.Tools.Actions;

/// <summary>
/// The record-level scope each connector action must pass, exactly as its HTTP endpoint does. An
/// Authorisation class answers "may this kind of user do this?"; the endpoint then asks the
/// feature's scope "is this record theirs?" before the handler runs. The gateway runs the same
/// scope here, so an external login connected through MCP reaches no further than it does on the
/// site (found by the security review, 2026-09-21: perform_action ran Allows and Check and
/// nothing else). A command with no entry here has no scope on its endpoint either — the
/// permission check's connector rule keeps the two lists in step.
/// </summary>
internal static class AiActionScopes
{
    private delegate Task<bool> ScopeCheck(JpmsContext context, SignedInUser user, object command, CancellationToken cancellationToken);

    private static readonly IReadOnlyDictionary<Type, ScopeCheck> Checks = new Dictionary<Type, ScopeCheck>
    {
        [typeof(ApproveVariationOrder)] = (context, user, command, cancellationToken) =>
            VariationOrderScope.MayActOnAsync(context, user, ((ApproveVariationOrder)command).VariationOrderId, cancellationToken),
        [typeof(RejectVariationOrder)] = (context, user, command, cancellationToken) =>
            VariationOrderScope.MayActOnAsync(context, user, ((RejectVariationOrder)command).VariationOrderId, cancellationToken),
        [typeof(PostVariationOrderMessage)] = (context, user, command, cancellationToken) =>
            VariationOrderScope.MayActOnAsync(context, user, ((PostVariationOrderMessage)command).VariationOrderId, cancellationToken),
        [typeof(RaiseRequest)] = (context, user, command, cancellationToken) =>
            RequestScope.MayRaiseOnProjectAsync(context, user, ((RaiseRequest)command).ProjectId, cancellationToken),
        [typeof(UpdateRequestDetails)] = (context, user, command, cancellationToken) =>
            RequestScope.MayActOnAsync(context, user, ((UpdateRequestDetails)command).RequestId, cancellationToken),
        [typeof(UpdateRequestForm)] = (context, user, command, cancellationToken) =>
            RequestScope.MayActOnAsync(context, user, ((UpdateRequestForm)command).RequestId, cancellationToken),
        [typeof(PostRequestMessage)] = (context, user, command, cancellationToken) =>
            RequestScope.MayActOnAsync(context, user, ((PostRequestMessage)command).RequestId, cancellationToken),
        [typeof(RaiseDefect)] = (context, user, command, cancellationToken) =>
            DefectScope.IsOnTheirOwnProjectAsync(context, user, ((RaiseDefect)command).ProjectId, cancellationToken),
        [typeof(SubmitQuoteForBidPackage)] = (context, user, command, cancellationToken) =>
            QuoteScope.MayPriceBidPackageAsync(context, user, ((SubmitQuoteForBidPackage)command).BidPackageId,
                ((SubmitQuoteForBidPackage)command).SubcontractorId, cancellationToken),
        [typeof(ReviseQuote)] = (context, user, command, cancellationToken) =>
            QuoteScope.MayReviseQuoteAsync(context, user, ((ReviseQuote)command).QuoteId, cancellationToken),
        [typeof(ImportArchitectInstructionFromMessage)] = (context, user, command, cancellationToken) =>
            ArchitectInstructionScope.MayFileOnProjectAsync(context, user, ((ImportArchitectInstructionFromMessage)command).ProjectId, cancellationToken),
        [typeof(UpdateArchitectInstruction)] = (context, user, command, cancellationToken) =>
            ArchitectInstructionScope.MayActOnAsync(context, user, ((UpdateArchitectInstruction)command).ArchitectInstructionId, cancellationToken),
        [typeof(LinkArchitectInstructionToVariation)] = (context, user, command, cancellationToken) =>
            ArchitectInstructionScope.MayActOnAsync(context, user, ((LinkArchitectInstructionToVariation)command).ArchitectInstructionId, cancellationToken),
        [typeof(UnlinkArchitectInstructionFromVariation)] = (context, user, command, cancellationToken) =>
            ArchitectInstructionScope.MayActOnAsync(context, user, ((UnlinkArchitectInstructionFromVariation)command).ArchitectInstructionId, cancellationToken),
        [typeof(DeleteArchitectInstruction)] = (context, user, command, cancellationToken) =>
            ArchitectInstructionScope.MayActOnAsync(context, user, ((DeleteArchitectInstruction)command).ArchitectInstructionId, cancellationToken),
        [typeof(SetFormSubmissionStatus)] = (context, user, command, cancellationToken) =>
            FormRecordScope.MayActOnFormAsync(context, user, ((SetFormSubmissionStatus)command).FormSubmissionId, cancellationToken),
        [typeof(FileFormToDirectory)] = (context, user, command, cancellationToken) =>
            FormRecordScope.MayActOnFormAsync(context, user, ((FileFormToDirectory)command).FormSubmissionId, cancellationToken),
        [typeof(RecordFormFolderDates)] = (context, user, command, cancellationToken) =>
            FormRecordScope.MayDateFolderAsync(context, user, ((RecordFormFolderDates)command).FormFolderId, cancellationToken),
    };

    public static IEnumerable<Type> ScopedCommands => Checks.Keys;

    public static Task<bool> AllowsAsync(JpmsContext context, SignedInUser user, object command, CancellationToken cancellationToken)
    {
        var isScoped = Checks.TryGetValue(command.GetType(), out var check);
        if (!isScoped) return Task.FromResult(true);
        return check!(context, user, command, cancellationToken);
    }
}

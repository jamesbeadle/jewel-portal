
namespace Jewel.JPMS.Api.Features.Manual.Commands;

/// <summary>One gate class, one Allows per command: the office maintains, the approvers approve, everyone acknowledges.</summary>
public sealed class ManualAuthorisationGate
{
    public bool Allows(SignedInUser user, CreateManualModule command) => ManualRoles.AllowedToManage.IncludesAny(user.Roles);

    public bool Allows(SignedInUser user, UpdateManualModuleDraft command) => ManualRoles.AllowedToManage.IncludesAny(user.Roles);

    public bool Allows(SignedInUser user, SubmitManualModuleForReview command) => ManualRoles.AllowedToManage.IncludesAny(user.Roles);

    public bool Allows(SignedInUser user, ApproveManualModule command) => ManualRoles.AllowedToApprove.IncludesAny(user.Roles);

    public bool Allows(SignedInUser user, ReturnManualModuleToDraft command) => ManualRoles.AllowedToApprove.IncludesAny(user.Roles);

    public bool Allows(SignedInUser user, ReviseManualModule command) => ManualRoles.AllowedToManage.IncludesAny(user.Roles);

    public bool Allows(SignedInUser user, RetireManualModule command) => ManualRoles.AllowedToApprove.IncludesAny(user.Roles);

    public bool Allows(SignedInUser user, AcknowledgeManualModule command) => ManualRoles.AllowedToReadViews.IncludesAny(user.Roles);

    public bool Allows(SignedInUser user, ImportManualBaseline command) => ManualRoles.AllowedToManage.IncludesAny(user.Roles);
}

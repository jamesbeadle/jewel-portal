using Jewel.JPMS.Api.Features.Registers.Policies;
using Jewel.JPMS.Contracts.Forms;
using Jewel.JPMS.Contracts.Registers;

namespace Jewel.JPMS.Api.Features.Ai.Tools.Actions;

internal sealed partial class FormsActions
{
    private static AiAction ChasePolicySignOffAction() => new AiAction(
        Name: "chase_policy_sign_off",
        Area: Area,
        Description: "SENDS EMAIL chasing one outstanding policy signature with a Policy sign-off link: the link already out is "
            + "sent again with a fresh expiry (the old one stops working), or someone asked on their portal login gets their "
            + "first link. Refused once they have signed, and once the revision is superseded — send the current one instead.",
        CommandType: typeof(ChasePolicySignOff),
        ResultType: typeof(SentFormLink),
        AuthorisationType: typeof(ChasePolicySignOffAuthorisation),
        ValidationType: typeof(ChasePolicySignOffValidation),
        VisibleTo: FormRoleSets.Office,
        EmailStamps: new[] { "SentByEmail" },
        NameStamps: new[] { "SentByName" },
        Notes: "policySignOffId comes from list_policy_sign_offs (outstanding[]). Name the person and the policy revision before the user confirms.",
        RequiresConfirmation: true);
}

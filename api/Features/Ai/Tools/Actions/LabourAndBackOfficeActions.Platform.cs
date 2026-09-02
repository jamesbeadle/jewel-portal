using Jewel.JPMS.Api.Features.Ai.Skills;
using Jewel.JPMS.Api.Features.Platform.Commands;
using Jewel.JPMS.Contracts.Ai;
using Jewel.JPMS.Contracts.Platform;

namespace Jewel.JPMS.Api.Features.Ai.Tools.Actions;

internal sealed partial class LabourAndBackOfficeActions
{
    private static IEnumerable<AiAction> PlatformActions() => new AiAction[]
    {
        new AiAction(
            Name: "publish_app_version",
            Area: "Platform",
            Description: "Bumps the announced app version by one, which raises the update toast on "
                + "EVERY open portal tab and prompts every signed-in user to refresh. Carries no "
                + "target number — one call, one increment, no way to move the number backwards.",
            CommandType: typeof(PublishAppVersion),
            ResultType: typeof(AnnouncedAppVersion),
            AuthorisationType: typeof(PublishAppVersionAuthorisation),
            ValidationType: typeof(PublishAppVersionValidation),
            VisibleTo: AdminGateRoles,
            EmailStamps: new[] { "PublishedBy" },
            NameStamps: Array.Empty<string>(),
            Notes: "Affects every user's open session at once and cannot be undone — confirm with "
                + "the user before calling."),

        new AiAction(
            Name: "attach_action_skills",
            Area: "Platform",
            Description: "Replaces the set of skills attached to one connector action or to a whole "
                + "action area — the wiring the AI Actions admin page edits. An attached skill's "
                + "doctrine is served by describe_action with that action's contract from the very "
                + "next call. An empty skill list detaches everything from the target.",
            CommandType: typeof(SaveAiActionSkills),
            ResultType: typeof(Acknowledgement),
            AuthorisationType: typeof(SaveAiActionSkillsAuthorisation),
            ValidationType: typeof(SaveAiActionSkillsValidation),
            VisibleTo: SkillRoles.ManageSkills,
            EmailStamps: new[] { "SavedByEmail" },
            NameStamps: Array.Empty<string>(),
            Notes: "targetKind is \"action\" or \"area\"; targetKey is the action name or the area "
                + "exactly as list_actions shows it; skillKeys come from list_skills. The save "
                + "REPLACES the target's whole set, so include every skill that should remain "
                + "attached, not just the one being added."),
    };
}

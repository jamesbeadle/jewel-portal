using Jewel.JPMS.Contracts.Ai;
using Microsoft.Extensions.DependencyInjection;

namespace Jewel.JPMS.Api.Features.Ai.Tools;

internal static partial class AiSkillHistoryTools
{
    private static AiTool RestoreSkillVersion() => new(
        "restore_skill_version",
        "WRITE: bring an earlier version of a stored skill (or of one of its reference documents) back "
        + "into force — the History panel's Restore on the AI Skills page. The earlier version's name, "
        + "description and text are saved as a NEW version, so the version it replaces stays in the "
        + "history and the restore can itself be undone; a skill keeps its discipline, pin and active "
        + "flag as they are now. Read the version first (list_skill_history with version), show the "
        + "user what changes, and restore only on their yes.",
        AiToolSchema.Object(
            ("skill_key", "string", "The skill's key.", true),
            ("ref_key", "string", "A reference document under the skill; leave out for the skill itself.", false),
            ("version", "integer", "The version to bring back, from list_skill_history.", true)),
        AiToolKind.Write,
        SkillRoles.ManageSkills,
        async (context, input, ct) =>
        {
            var command = new RestoreAiSkillVersion(
                AiToolSchema.Text(input, "skill_key")?.Trim() ?? "",
                AiToolSchema.Text(input, "ref_key")?.Trim(),
                AiToolSchema.Number(input, "version") ?? 0,
                RestoredByEmail: context.User.Email);

            var authorisation = context.Services.GetRequiredService<Skills.RestoreAiSkillVersionAuthorisation>();
            if (!authorisation.Allows(context.User, command))
                return Fail("Your portal roles do not allow this action.");
            var validation = context.Services.GetRequiredService<Skills.RestoreAiSkillVersionValidation>().Check(command);
            if (validation.HasFailed) return Serialise(new { ok = false, errors = validation.Errors });

            try
            {
                await context.Services
                    .GetRequiredService<ICommandHandler<RestoreAiSkillVersion, Acknowledgement>>()
                    .HandleAsync(command, ct);
            }
            catch (InvalidOperationException refusal)
            {
                return Fail(refusal.Message);
            }

            return Serialise(new { ok = true, skill = command.SkillKey, refKey = command.RefKey, restored = command.Version,
                note = "Restored as a new version — live for the whole team from the next conversation." });
        });
}

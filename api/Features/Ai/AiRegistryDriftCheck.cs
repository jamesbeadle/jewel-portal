using Jewel.JPMS.Api.Features.Ai.Tools;
using Jewel.JPMS.Contracts.Ai;

namespace Jewel.JPMS.Api.Features.Ai;

/// <summary>
/// The assistant is held together by several hand-kept registries that must agree: the tool
/// catalogue, ModalCatalog, the open_modal tool's own description of the dialogs, and the status
/// labels the panel shows. They have drifted before — select_email shipped half-wired, page
/// guides lagged new dialogs, labels missed the slowest tools — and every drift surfaces as the
/// assistant confidently narrating something that never happened.
///
/// <para>This check runs once at registration and THROWS on drift. That is deliberate: the
/// registries are static data compiled into this assembly, so the check is deterministic — it
/// cannot fail intermittently, and it cannot pass locally then fail deployed. Failing the boot is
/// the point: a drifted registry never reaches a user.</para>
/// </summary>
public static class AiRegistryDriftCheck
{
    public static void Assert()
    {
        var complaints = new List<string>();

        var openModal = AiToolCatalogue.All.FirstOrDefault(tool =>
            string.Equals(tool.Name, "open_modal", StringComparison.OrdinalIgnoreCase));
        if (openModal is null) complaints.Add("open_modal is not in the tool catalogue.");

        // Page-anchored dialogs are deliberately NOT openable via open_modal — the page supplies
        // their anchor (tender_reply's tender email) when it starts the task itself.
        var pageAnchored = new[] { ModalCatalog.TenderReply.ModalKey };

        foreach (var modal in ModalCatalog.All)
        {
            if (pageAnchored.Contains(modal.ModalKey, StringComparer.OrdinalIgnoreCase)) continue;

            if (openModal is not null
                && !openModal.Description.Contains($"\"{modal.ModalKey}\"", StringComparison.Ordinal))
            {
                complaints.Add(
                    $"open_modal's description never mentions \"{modal.ModalKey}\" — the model is "
                    + "never told the dialog exists. Describe it there (and in the modal_key "
                    + "enum) in the same commit that registers a dialog.");
            }

            if (AiToolLabels.For("open_modal", $"{{\"modal_key\":\"{modal.ModalKey}\"}}") == "Opening a dialog")
            {
                complaints.Add(
                    $"AiToolLabels has no open_modal line for \"{modal.ModalKey}\" — the user "
                    + "watches the generic \"Opening a dialog\" instead of what is happening.");
            }
        }

        foreach (var tool in AiToolCatalogue.All)
        {
            if (AiToolLabels.For(tool.Name, null) == "Working on it")
            {
                complaints.Add(
                    $"AiToolLabels has no label for \"{tool.Name}\" — the user watches "
                    + "\"Working on it\" for its whole run, and the slow tools run longest.");
            }
        }

        // The evidence rule must name every source-reading tool, and every tool it names must
        // exist — a reader the prompt never mentions is a reader the model never reaches for, and
        // a name the prompt mentions that the catalogue lacks is a model promising a call it
        // cannot make.
        foreach (var name in AiSourceTools.Names)
        {
            if (!AiSystemPrompt.EvidenceRule.Contains(name, StringComparison.Ordinal))
                complaints.Add($"AiSystemPrompt.EvidenceRule never mentions \"{name}\" — the model is never told to use it.");
            if (!AiToolCatalogue.All.Any(tool => string.Equals(tool.Name, name, StringComparison.OrdinalIgnoreCase)))
                complaints.Add($"AiSourceTools.Names lists \"{name}\" but the tool catalogue has no such tool.");
        }

        if (complaints.Count > 0)
        {
            throw new InvalidOperationException(
                "The AI registries have drifted out of step:\n- " + string.Join("\n- ", complaints));
        }
    }
}
